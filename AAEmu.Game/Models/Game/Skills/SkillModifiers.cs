using System.Collections.Generic;
using System.Linq;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills{    public class SkillModifiers
    {
        public uint Id { get; set; }
        public uint OwnerId { get; set; }
        public string OwnerType { get; set; }
        public uint TagId { get; set; }
        public SkillAttributeType SkillAttribute { get; set; }
        public UnitModifierType UnitModifierType { get; set; }
        public int Value { get; set; }
        public uint SkillId { get; set; }
        public bool Synergy { get; set; }

        private readonly Dictionary<uint, List<SkillModifiers>> _modifiersBySkillId;
        private readonly Dictionary<uint, List<SkillModifiers>> _modifiersByTagId;

        public SkillModifiers()
        {
            _modifiersBySkillId = new Dictionary<uint, List<SkillModifiers>>();
            _modifiersByTagId = new Dictionary<uint, List<SkillModifiers>>();
        }

        public double ApplyModifiers(Skill skill, SkillAttributeType attribute, double baseValue)
        {
            var endValue = baseValue;

            var modifiers = GetModifiersForSkillIdWithAttribute(skill.Template.Id, attribute).OrderBy(mod => mod.UnitModifierType).ToList();

            foreach (var tag in SkillManager.Instance.GetSkillTags(skill.Template.Id))
            {
                modifiers.AddRange(GetModifiersForTagIdWithAttribute(tag, attribute));
            }

            foreach (var modifier in modifiers)
            {
                switch (modifier.UnitModifierType)
                {
                    case UnitModifierType.Percent:
                        endValue += endValue * (modifier.Value / 100);
                        break;
                    case UnitModifierType.Value:
                        endValue += modifier.Value;
                        break;
                }
            }

            return endValue;
        }

        public List<SkillModifiers> GetModifiersForSkillIdWithAttribute(uint skillId, SkillAttributeType attribute)
        {
            var modifiers = GetModifiersForSkillId(skillId);
            if (modifiers == null) return new List<SkillModifiers>();
            return modifiers.Where(mod => mod.SkillAttribute == attribute).ToList();
        }

        public List<SkillModifiers> GetModifiersForTagIdWithAttribute(uint tagId, SkillAttributeType attribute)
        {
            var modifiers = GetModifiersForTagId(tagId);
            if (modifiers == null) return new List<SkillModifiers>();
            return modifiers.Where(mod => mod.SkillAttribute == attribute).ToList();
        }

        public List<SkillModifiers> GetModifiersForSkillId(uint skillId)
        {
            if (_modifiersBySkillId.ContainsKey(skillId))
                return _modifiersBySkillId[skillId];
            return null;
        }

        public List<SkillModifiers> GetModifiersForTagId(uint tagId)
        {
            if (_modifiersByTagId.ContainsKey(tagId))
                return _modifiersByTagId[tagId];
            return null;
        }

        public void AddModifiers(uint ownerId)
        {
            var modifiers = SkillManager.Instance.GetModifiersByOwnerId(ownerId);
            if (modifiers != null)
            {
                foreach (var modifier in modifiers)
                {
                    AddModifier(modifier);
                }
            }
        }

        public void RemoveModifiers(uint ownerId)
        {
            var modifiers = SkillManager.Instance.GetModifiersByOwnerId(ownerId);
            foreach (var modifier in modifiers)
            {
                RemoveModifier(modifier);
            }
        }

        public void AddModifier(SkillModifiers modifier)
        {
            if (modifier.SkillId > 0)
            {
                if (!_modifiersBySkillId.ContainsKey(modifier.SkillId))
                    _modifiersBySkillId.Add(modifier.SkillId, new List<SkillModifiers>());
                _modifiersBySkillId[modifier.SkillId].Add(modifier);
            }

            if (modifier.TagId > 0)
            {
                if (!_modifiersByTagId.ContainsKey(modifier.TagId))
                    _modifiersByTagId.Add(modifier.TagId, new List<SkillModifiers>());
                _modifiersByTagId[modifier.TagId].Add(modifier);
            }
        }

        public void RemoveModifier(SkillModifiers modifier)
        {
            if (_modifiersBySkillId.ContainsKey(modifier.SkillId))
                _modifiersBySkillId[modifier.SkillId].Remove(modifier);

            if (_modifiersBySkillId.ContainsKey(modifier.TagId))
                _modifiersByTagId[modifier.TagId].Remove(modifier);
        }
    }
}
