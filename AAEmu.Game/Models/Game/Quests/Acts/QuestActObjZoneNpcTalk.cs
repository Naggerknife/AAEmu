﻿using AAEmu.Game.Models.Game.Quests.Templates;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Models.Game.Quests.Acts
{
    public class QuestActObjZoneNpcTalk : QuestActTemplate
    {
        public uint NpcId { get; set; }
        public bool UseAlias { get; set; }
        public uint QuestActObjAliasId { get; set; }

        public override bool Use(Character character, Quest quest, int objective)
        {
            _log.Warn("QuestActObjZoneNpcTalk QuestId {0}, NpcId {1}, UseAlias {2}, QuestActObjAliasId {3}, objective {4}",
                quest.TemplateId, NpcId, UseAlias, QuestActObjAliasId, objective);

            return false;
        }
    }
}
