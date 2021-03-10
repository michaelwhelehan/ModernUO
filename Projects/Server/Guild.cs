using System.Collections.Generic;

namespace Server.Guilds
{
    public enum GuildType
    {
        Regular,
        Chaos,
        Order
    }

    public abstract class BaseGuild : ISerializable
    {
        protected BaseGuild(Serial serial)
        {
            Serial = serial;

            var ourType = GetType();
            TypeRef = World.GuildTypes.IndexOf(ourType);

            if (TypeRef == -1)
            {
                World.GuildTypes.Add(ourType);
                TypeRef = World.GuildTypes.Count - 1;
            }
        }

        protected BaseGuild()
        {
            Serial = World.NewGuild;
            World.AddGuild(this);

            var ourType = GetType();
            TypeRef = World.GuildTypes.IndexOf(ourType);

            if (TypeRef == -1)
            {
                World.GuildTypes.Add(ourType);
                TypeRef = World.GuildTypes.Count - 1;
            }
        }

        public abstract string Abbreviation { get; set; }
        public abstract string Name { get; set; }
        public abstract GuildType Type { get; set; }
        public abstract bool Disbanded { get; }
        public abstract void Delete();
        public abstract void MarkDirty();

        public bool Deleted => Disbanded;

        [CommandProperty(AccessLevel.Counselor)]
        public Serial Serial { get; }

        BufferWriter ISerializable.SaveBuffer { get; set; }

        public int TypeRef { get; }

        public abstract void Serialize(IGenericWriter writer);
        public abstract void Deserialize(IGenericReader reader);
        public abstract void OnDelete(Mobile mob);

        public static BaseGuild FindByName(string name)
        {
            foreach (var g in World.Guilds.Values)
            {
                if (g.Name == name)
                {
                    return g;
                }
            }

            return null;
        }

        public static BaseGuild FindByAbbrev(string abbr)
        {
            foreach (var g in World.Guilds.Values)
            {
                if (g.Abbreviation == abbr)
                {
                    return g;
                }
            }

            return null;
        }

        public static HashSet<BaseGuild> Search(string find)
        {
            var words = find.ToLower().Split(' ');
            var results = new HashSet<BaseGuild>();

            foreach (var g in World.Guilds.Values)
            {
                var name = g.Name;

                bool all = true;
                foreach (var t in words)
                {
                    if (name.InsensitiveIndexOf(t) == -1)
                    {
                        all = false;
                        break;
                    }
                }

                if (all)
                {
                    results.Add(g);
                }
            }

            return results;
        }

        public override string ToString() => $"0x{Serial.Value:X} \"{Name} [{Abbreviation}]\"";
    }
}
