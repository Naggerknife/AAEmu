using System.Collections.Generic;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Skills;
using MySql.Data.MySqlClient;

namespace AAEmu.Game.Models.Game.Char
{
    public class CharacterAbilities
    {
        public Dictionary<AbilityType, Ability> Abilities { get; set; }
        public Character Owner { get; set; }

        public CharacterAbilities(Character owner)
        {
            Owner = owner;
            Abilities = new Dictionary<AbilityType, Ability>();
            for (var i = 1; i < 11; i++)
            {
                var id = (AbilityType) i;
                Abilities[id] = new Ability(id);
            }
        }

        public IEnumerable<Ability> Values => Abilities.Values;

        public void SetAbility(AbilityType id, byte order)
        {
            Abilities[id].Order = order;
        }

        public List<AbilityType> GetActiveAbilities()
        {
            var list = new List<AbilityType>();
            if (Owner.SkillTreeOne != AbilityType.None)
                list.Add(Owner.SkillTreeOne);
            if (Owner.SkillTreeTwo != AbilityType.None)
                list.Add(Owner.SkillTreeTwo);
            if (Owner.SkillTreeThree != AbilityType.None)
                list.Add(Owner.SkillTreeThree);
            return list;
        }

        public void AddExp(AbilityType type, int exp)
        {
            // TODO SCAbilityExpChangedPacket
            if (type != AbilityType.None)
                Abilities[type].Exp += exp;
        }

        public void AddActiveExp(int exp)
        {
            // TODO SCExpChangedPacket
            if (Owner.SkillTreeOne != AbilityType.None)
                Abilities[Owner.SkillTreeOne].Exp += exp;
            if (Owner.SkillTreeTwo != AbilityType.None)
                Abilities[Owner.SkillTreeTwo].Exp += exp;
            if (Owner.SkillTreeThree != AbilityType.None)
                Abilities[Owner.SkillTreeThree].Exp += exp;
        }

        public void Swap(AbilityType oldAbilityId, AbilityType abilityId)
        {
            if (Owner.SkillTreeOne == oldAbilityId)
            {
                Owner.SkillTreeOne = abilityId;
                Abilities[abilityId].Order = 0;
            }
            else if (Owner.SkillTreeTwo == oldAbilityId)
            {
                Owner.SkillTreeTwo = abilityId;
                Abilities[abilityId].Order = 1;
            }
            else if (Owner.SkillTreeThree == oldAbilityId)
            {
                Owner.SkillTreeThree = abilityId;
                Abilities[abilityId].Order = 2;
            }

            if (oldAbilityId != AbilityType.None)
                Abilities[oldAbilityId].Order = 255;
            Owner.BroadcastPacket(new SCAbilitySwappedPacket(Owner.ObjId, oldAbilityId, abilityId), true);
        }

        public void Load(MySqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM abilities WHERE `owner` = @owner";
                command.Parameters.AddWithValue("@owner", Owner.Id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ability = new Ability
                        {
                            Id = (AbilityType) reader.GetByte("id"),
                            Exp = reader.GetInt32("exp")
                        };
                        if (ability.Id == Owner.SkillTreeOne)
                            ability.Order = 0;
                        if (ability.Id == Owner.SkillTreeTwo)
                            ability.Order = 1;
                        if (ability.Id == Owner.SkillTreeThree)
                            ability.Order = 2;
                        Abilities[ability.Id] = ability;
                    }
                }
            }
        }

        public void Save(MySqlConnection connection, MySqlTransaction transaction)
        {
            foreach (var ability in Abilities.Values)
            {
                using (var command = connection.CreateCommand())
                {
                    command.Connection = connection;
                    command.Transaction = transaction;

                    command.CommandText = "REPLACE INTO abilities(`id`,`exp`,`owner`) VALUES (@id, @exp, @owner)";
                    command.Parameters.AddWithValue("@id", (byte) ability.Id);
                    command.Parameters.AddWithValue("@exp", ability.Exp);
                    command.Parameters.AddWithValue("@owner", Owner.Id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
