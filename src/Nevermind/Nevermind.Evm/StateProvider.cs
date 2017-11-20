using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Nevermind.Core;
using Nevermind.Core.Encoding;
using Nevermind.Store;

namespace Nevermind.Evm
{
    public class StateProvider : IStateProvider
    {
        private const int StartCapacity = 1024;

        private readonly Dictionary<Address, Stack<int>> _cache = new Dictionary<Address, Stack<int>>();
        private readonly Dictionary<Keccak, byte[]> _code = new Dictionary<Keccak, byte[]>();

        private readonly HashSet<Address> _committedThisRound = new HashSet<Address>();

        private readonly List<Change> _keptInCache = new List<Change>();
        private readonly IProtocolSpecification _protocolSpecification;
        private readonly ILogger _logger;

        private int _capacity = StartCapacity;
        private Change[] _changes = new Change[StartCapacity];
        private int _currentPosition = -1;

        public StateProvider(StateTree stateTree, IProtocolSpecification protocolSpecification, ILogger logger)
        {
            _protocolSpecification = protocolSpecification;
            _logger = logger;
            State = stateTree;
        }

        public StateTree State { get; }

        public bool AccountExists(Address address)
        {
            if (_cache.ContainsKey(address))
            {
                return _changes[_cache[address].Peek()].ChangeType != ChangeType.Delete;
            }

            return GetAndAddToCache(address) != null;
        }

        public bool IsEmptyAccount(Address address)
        {
            return GetThroughCache(address).IsEmpty;
        }

        public BigInteger GetNonce(Address address)
        {
            Account account = GetThroughCache(address);
            return account?.Nonce ?? BigInteger.Zero;
        }

        public BigInteger GetBalance(Address address)
        {
            Account account = GetThroughCache(address);
            return account?.Balance ?? BigInteger.Zero;
        }

        public void UpdateCodeHash(Address address, Keccak codeHash)
        {
            _logger?.Log($"  SETTING CODE HASH of {address} to {codeHash}");

            Account account = GetThroughCache(address);
            if (account.CodeHash != codeHash)
            {
                Account changedAccount = account.WithChangedCodeHash(codeHash);
                PushUpdate(address, changedAccount);
            }
        }

        public void UpdateBalance(Address address, BigInteger balanceChange)
        {
            if (balanceChange == BigInteger.Zero && !_protocolSpecification.IsEip158Enabled)
            {
                return;
            }

            Account account = GetThroughCache(address);
            if (_protocolSpecification.IsEip158Enabled && balanceChange == BigInteger.Zero && account.IsEmpty)
            {
                PushDelete(address);
            }
            else
            {
                BigInteger newbalance = account.Balance + balanceChange;
                if (newbalance < 0)
                {
                    throw new InsufficientBalanceException();
                }

                Account changedAccount = account.WithChangedBalance(account.Balance + balanceChange);
                _logger?.Log($"  UPDATE {address} B = {account.Balance + balanceChange} B_CHANGE = {balanceChange}");

                PushUpdate(address, changedAccount);
            }
        }

        public void UpdateStorageRoot(Address address, Keccak storageRoot)
        {
            Account account = GetThroughCache(address);
            if (account.StorageRoot == storageRoot)
            {
                return;
            }

            if (account.StorageRoot != storageRoot) // TODO: will it ever be called? unnecessary comp
            {
                Account changedAccount = account.WithChangedStorageRoot(storageRoot);
                PushUpdate(address, changedAccount);
            }
        }

        public void IncrementNonce(Address address)
        {
            //if (ShouldLog.State) Console.WriteLine($"  SETTING NONCE of {address}");

            Account account = GetThroughCache(address);
            Account changedAccount = account.WithChangedNonce(account.Nonce + 1);
            PushUpdate(address, changedAccount);
        }

        public Keccak UpdateCode(byte[] code)
        {
            if (code.Length == 0)
            {
                return Keccak.OfAnEmptyString;
            }

            Keccak codeHash = Keccak.Compute(code);
            _code[codeHash] = code;

            return codeHash;
        }

        public byte[] GetCode(Keccak codeHash)
        {
            if (codeHash == Keccak.OfAnEmptyString)
            {
                return new byte[0];
            }

            return _code[codeHash];
        }

        public byte[] GetCode(Address address)
        {
            Account account = GetThroughCache(address);
            if (account == null)
            {
                return new byte[0];
            }

            return GetCode(account.CodeHash);
        }

        public void DeleteAccount(Address address)
        {
            PushDelete(address);
        }

        public int TakeSnapshot()
        {
            return _currentPosition;
        }

        public void Restore(int snapshot)
        {
            _logger?.Log($"  RESTORING SNAPSHOT {snapshot}");

            for (int i = 0; i < _currentPosition - snapshot; i++)
            {
                Change change = _changes[_currentPosition - i];
                if (_cache[change.Address].Count == 1)
                {
                    if (change.ChangeType == ChangeType.JustCache)
                    {
                        int actualPosition = _cache[change.Address].Pop();
                        Debug.Assert(_currentPosition - i == actualPosition);
                        _keptInCache.Add(change);
                        _changes[actualPosition] = null;
                        continue;
                    }
                }

                _changes[_currentPosition - i] = null; // TODO: temp
                int forAssertion = _cache[change.Address].Pop();
                Debug.Assert(forAssertion == _currentPosition - i);

                if (_cache[change.Address].Count == 0)
                {
                    _cache.Remove(change.Address);
                }
            }

            _currentPosition = snapshot;
            foreach (Change kept in _keptInCache)
            {
                _currentPosition++;
                _changes[_currentPosition] = kept;
                _cache[kept.Address].Push(_currentPosition);
            }

            _keptInCache.Clear();
        }

        public void CreateAccount(Address address, BigInteger balance)
        {
            _logger?.Log($"  CREATING ACCOUNT: {address} with balance {balance}");

            Account account = new Account();
            account.Balance = balance;
            PushNew(address, account);
        }

        public void Commit()
        {
            _logger?.Log("  COMMITTING CHANGES");

            if (_currentPosition == -1)
            {
                return;
            }

            Debug.Assert(_changes[_currentPosition] != null);
            Debug.Assert(_changes[_currentPosition + 1] == null);

            for (int i = 0; i <= _currentPosition; i++)
            {
                Change change = _changes[_currentPosition - i];
                if (_committedThisRound.Contains(change.Address))
                {
                    continue;
                }

                int forAssertion = _cache[change.Address].Pop();
                Debug.Assert(forAssertion == _currentPosition - i);

                _committedThisRound.Add(change.Address);

                switch (change.ChangeType)
                {
                    case ChangeType.JustCache:
                    {
                        break;
                    }
                    case ChangeType.Update:
                    {
                        _logger?.Log($"  UPDATE {change.Address} B = {change.Account.Balance} N = {change.Account.Nonce}");

                        if (_protocolSpecification.IsEip158Enabled && change.Account.IsEmpty)
                        {
                            State.Set(change.Address, null);
                        }
                        else
                        {
                            State.Set(change.Address, Rlp.Encode(change.Account));
                        }

                        break;
                    }
                    case ChangeType.New:
                    {
                        _logger?.Log($"  CREATE {change.Address} B = {change.Account.Balance} N = {change.Account.Nonce}");

                        if (!_protocolSpecification.IsEip158Enabled || !change.Account.IsEmpty)
                        {
                            State.Set(change.Address, Rlp.Encode(change.Account));
                        }

                        break;
                    }
                    case ChangeType.Delete:
                    {
                        _logger?.Log($"  DELETE {change.Address}");

                        bool wasItCreatedNow = false;
                        while (_cache[change.Address].Count > 0)
                        {
                            int previousOne = _cache[change.Address].Pop();
                            wasItCreatedNow |= _changes[previousOne].ChangeType == ChangeType.New;
                            if (wasItCreatedNow)
                            {
                                break;
                            }
                        }

                        if (!wasItCreatedNow)
                        {
                            State.Set(change.Address, null);
                        }
                        break;
                    }
                    default:
                    throw new ArgumentOutOfRangeException();
                }
            }

            _capacity = 1024;
            _changes = new Change[_capacity];
            _currentPosition = -1;
            _committedThisRound.Clear();
            _cache.Clear();
        }

        private Account GetAccount(Address address)
        {
            Rlp rlp = State.Get(address);
            if (rlp.Bytes == null)
            {
                return null;
            }

            return Rlp.Decode<Account>(rlp);
        }

        private Account GetAndAddToCache(Address address)
        {
            Account account = GetAccount(address);
            if (account != null)
            {
                PushJustCache(address, account);
            }

            return account;
        }

        private Account GetThroughCache(Address address)
        {
            if (_cache.ContainsKey(address))
            {
                return _changes[_cache[address].Peek()].Account;
            }

            Account account = GetAndAddToCache(address);
            return account;
        }

        private void PushJustCache(Address address, Account changedAccount)
        {
            SetupCache(address);
            IncrementPosition();
            _cache[address].Push(_currentPosition);
            _changes[_currentPosition] = new Change(ChangeType.JustCache, address, changedAccount);
        }

        private void PushUpdate(Address address, Account changedAccount)
        {
            SetupCache(address);
            IncrementPosition();
            _cache[address].Push(_currentPosition);
            _changes[_currentPosition] = new Change(ChangeType.Update, address, changedAccount);
        }

        private void PushDelete(Address address)
        {
            SetupCache(address);
            IncrementPosition();
            _cache[address].Push(_currentPosition);
            _changes[_currentPosition] = new Change(ChangeType.Delete, address, null);
        }

        private void PushNew(Address address, Account account)
        {
            SetupCache(address);
            IncrementPosition();
            _cache[address].Push(_currentPosition);
            _changes[_currentPosition] = new Change(ChangeType.New, address, account);
        }

        private void IncrementPosition()
        {
            _currentPosition++;
            if (_currentPosition > _capacity - 1)
            {
                _capacity *= 2;
                Array.Resize(ref _changes, _capacity);
            }
        }

        private void SetupCache(Address address)
        {
            if (!_cache.ContainsKey(address))
            {
                _cache[address] = new Stack<int>();
            }
        }

        private enum ChangeType
        {
            JustCache,
            Update,
            New,
            Delete
        }

        private class Change
        {
            public Change(ChangeType type, Address address, Account account)
            {
                ChangeType = type;
                Address = address;
                Account = account;
            }

            public ChangeType ChangeType { get; }
            public Address Address { get; }
            public Account Account { get; }
        }
    }
}