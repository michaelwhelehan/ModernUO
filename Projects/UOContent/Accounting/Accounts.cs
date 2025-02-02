using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Accounting
{
    public static class Accounts
    {
        private static readonly Dictionary<string, IAccount> _accountsByName = new(32, StringComparer.OrdinalIgnoreCase);
        private static Dictionary<Serial, IAccount> _accountsById = new(32);
        private static Serial _lastAccount;
        internal static List<Type> Types { get; } = new();

        private static void OutOfMemory(string message) => throw new OutOfMemoryException(message);

        public static Serial NewAccount
        {
            get
            {
                uint last = _lastAccount;

                for (uint i = 0; i < uint.MaxValue; i++)
                {
                    last++;

                    if (FindAccount(last) == null)
                    {
                        return _lastAccount = last;
                    }
                }

                OutOfMemory("No serials left to allocate for accounts");
                return Serial.MinusOne;
            }
        }

        public static int Count => _accountsByName.Count;

        public static void Configure() =>
            Persistence.Register("Accounts", Serialize, WriteSnapshot, Deserialize);

        internal static void Serialize() =>
            EntityPersistence.SaveEntities(_accountsById.Values, account => account.Serialize());

        internal static void WriteSnapshot(string basePath)
        {
            IIndexInfo<Serial> indexInfo = new EntityTypeIndex("Accounts");
            EntityPersistence.WriteEntities(indexInfo, _accountsById, Types, basePath, out _);
        }

        public static IEnumerable<IAccount> GetAccounts() => _accountsByName.Values;

        public static IAccount GetAccount(string username)
        {
            _accountsByName.TryGetValue(username, out var a);
            return a;
        }

        public static void Add(IAccount a)
        {
            _accountsByName[a.Username] = a;
            _accountsById[a.Serial] = a;
        }

        public static void Remove(IAccount a)
        {
            _accountsByName.Remove(a.Username);
            _accountsById.Remove(a.Serial);
        }

        internal static void Deserialize(string path)
        {
            var filePath = Path.Combine(path, "Accounts", "accounts.xml");

            // Backward Compatibility
            if (File.Exists(filePath))
            {
                DeserializeXml(filePath);
                return;
            }

            IIndexInfo<Serial> indexInfo = new EntityTypeIndex("Accounts");

            _accountsById = EntityPersistence.LoadIndex(path, indexInfo, out List<EntityIndex<IAccount>> accounts);
            EntityPersistence.LoadData(path, indexInfo, accounts);

            foreach (var a in _accountsById.Values)
            {
                _accountsByName[a.Username] = a;
            }
        }

        private static void DeserializeXml(string filePath)
        {
            var doc = new XmlDocument();
            doc.Load(filePath);

            var root = doc["accounts"];

            if (root == null)
            {
                throw new FileLoadException("Unable to load xml file");
            }

            foreach (XmlElement account in root.GetElementsByTagName("account"))
            {
                try
                {
                    new Account(account);
                }
                catch
                {
                    Console.WriteLine("Warning: Account instance load failed");
                }
            }
        }

        public static IAccount FindAccount(Serial serial)
        {
            _accountsById.TryGetValue(serial, out var account);
            return account;
        }
    }
}
