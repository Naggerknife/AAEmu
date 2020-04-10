using System;
using System.Collections.Generic;
using System.Linq;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Error;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items.Templates;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Utils.DB;
using MySql.Data.MySqlClient;
using NLog;

namespace AAEmu.Game.Models.Game.Char
{
    public class Inventory
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        private int _freeSlot;
        private int _freeBankSlot;
        private List<ulong> _removedItems;

        public readonly Character Owner;
        private int MaxExpandCapacity = 130; //Max size of inventory and bank is 130 slots
        private uint ExpansionScrollId = 8000025; //Template Id of expansion scroll

        public Item[] Equip { get; set; }
        public Item[] Items { get; set; }
        public Item[] Bank { get; set; }

        public Inventory(Character owner)
        {
            Owner = owner;
            Equip = new Item[28];
            Items = new Item[Owner.NumInventorySlots];
            Bank = new Item[Owner.NumBankSlots];
            _removedItems = new List<ulong>();
        }

        #region Database

        public void Load(MySqlConnection connection, SlotType? slotType = null)
        {
            using (var command = connection.CreateCommand())
            {
                if (slotType == null)
                    command.CommandText = "SELECT * FROM items WHERE `owner` = @owner";
                else
                {
                    command.CommandText = "SELECT * FROM items WHERE `owner` = @owner AND `slot_type` = @slot_type";
                    command.Parameters.AddWithValue("@slot_type", slotType);
                }

                command.Parameters.AddWithValue("@owner", Owner.Id);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var type = reader.GetString("type");
                        Type nClass = null;
                        try
                        {
                            nClass = Type.GetType(type);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                        }

                        if (nClass == null)
                        {
                            _log.Error("Item type {0} not found!", type);
                            continue;
                        }

                        Item item;
                        try
                        {
                            item = (Item)Activator.CreateInstance(nClass);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                            _log.Error(ex.InnerException);
                            item = new Item();
                        }

                        item.Id = reader.GetUInt64("id");
                        item.TemplateId = reader.GetUInt32("template_id");
                        item.Template = ItemManager.Instance.GetTemplate(item.TemplateId);
                        item.SlotType = (SlotType)Enum.Parse(typeof(SlotType), reader.GetString("slot_type"), true);
                        item.Slot = reader.GetInt32("slot");
                        item.Count = reader.GetInt32("count");
                        item.LifespanMins = reader.GetInt32("lifespan_mins");
                        item.MadeUnitId = reader.GetUInt32("made_unit_id");
                        item.UnsecureTime = reader.GetDateTime("unsecure_time");
                        item.UnpackTime = reader.GetDateTime("unpack_time");
                        item.CreateTime = reader.GetDateTime("created_at");
                        var details = (PacketStream)(byte[])reader.GetValue("details");
                        item.ReadDetails(details);

                        if (item.Template.FixedGrade >= 0)
                            item.Grade = (byte)item.Template.FixedGrade; // Overwrite Fixed-grade items, just to make sure
                        else if (item.Template.Gradable)
                            item.Grade = reader.GetByte("grade"); // Load from our DB if the item is gradable

                        if (item.SlotType == SlotType.Equipment)
                            Equip[item.Slot] = item;
                        else if (item.SlotType == SlotType.Inventory)
                            Items[item.Slot] = item;
                        else if (item.SlotType == SlotType.Bank)
                            Bank[item.Slot] = item;
                    }
                }
            }

            if (slotType == null || slotType == SlotType.Equipment)
                foreach (var item in Equip.Where(x => x != null))
                    item.Template = ItemManager.Instance.GetTemplate(item.TemplateId);

            if (slotType == null || slotType == SlotType.Inventory)
            {
                foreach (var item in Items.Where(x => x != null))
                    item.Template = ItemManager.Instance.GetTemplate(item.TemplateId);
                _freeSlot = CheckFreeSlot(SlotType.Inventory);
            }

            if (slotType == null || slotType == SlotType.Bank)
            {
                foreach (var item in Bank.Where(x => x != null))
                    item.Template = ItemManager.Instance.GetTemplate(item.TemplateId);

                _freeBankSlot = CheckFreeSlot(SlotType.Bank);
            }
        }

        public void Save(MySqlConnection connection, MySqlTransaction transaction)
        {
            lock (_removedItems)
            {
                if (_removedItems.Count > 0)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM items WHERE owner= @owner AND id IN(" + string.Join(",", _removedItems) + ")";
                        command.Prepare();
                        command.Parameters.AddWithValue("@owner", Owner.Id);
                        command.ExecuteNonQuery();
                    }

                    _removedItems.Clear();
                }
            }

            SaveItems(connection, transaction, Equip);
            SaveItems(connection, transaction, Items);
            SaveItems(connection, transaction, Bank);
        }

        private void SaveItems(MySqlConnection connection, MySqlTransaction transaction, Item[] items)
        {
            using (var command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                foreach (var item in items)
                {
                    if (item == null)
                        continue;
                    var details = new PacketStream();
                    item.WriteDetails(details);

                    command.CommandText = "REPLACE INTO " +
                                          "items(`id`,`type`,`template_id`,`slot_type`,`slot`,`count`,`details`,`lifespan_mins`,`made_unit_id`,`unsecure_time`,`unpack_time`,`owner`,`created_at`,`grade`)" +
                                          " VALUES " +
                                          "(@id,@type,@template_id,@slot_type,@slot,@count,@details,@lifespan_mins,@made_unit_id,@unsecure_time,@unpack_time,@owner,@created_at,@grade)";

                    command.Parameters.AddWithValue("@id", item.Id);
                    command.Parameters.AddWithValue("@type", item.GetType().ToString());
                    command.Parameters.AddWithValue("@template_id", item.TemplateId);
                    command.Parameters.AddWithValue("@slot_type", (byte)item.SlotType);
                    command.Parameters.AddWithValue("@slot", item.Slot);
                    command.Parameters.AddWithValue("@count", item.Count);
                    command.Parameters.AddWithValue("@details", details.GetBytes());
                    command.Parameters.AddWithValue("@lifespan_mins", item.LifespanMins);
                    command.Parameters.AddWithValue("@made_unit_id", item.MadeUnitId);
                    command.Parameters.AddWithValue("@unsecure_time", item.UnsecureTime);
                    command.Parameters.AddWithValue("@unpack_time", item.UnpackTime);
                    command.Parameters.AddWithValue("@created_at", item.CreateTime);
                    command.Parameters.AddWithValue("@owner", Owner.Id);
                    command.Parameters.AddWithValue("@grade", item.Grade);
                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                }
            }
        }

        #endregion

        public void Send()
        {
            Owner.SendPacket(new SCCharacterInvenInitPacket(Owner.NumInventorySlots, (uint)Owner.NumBankSlots));
            SendFragmentedInventory(SlotType.Inventory, Owner.NumInventorySlots, Items);
            SendFragmentedInventory(SlotType.Bank, (byte)Owner.NumBankSlots, Bank);
        }

        public bool AddExistingItem(Item item, bool dontAddPartialCount, SlotType slotType, ItemTaskType reason)
        {
            var tasks = TryAddItem(item, slotType, dontAddPartialCount);
            if (tasks == null)
            {
                if (!dontAddPartialCount)
                    ItemIdManager.Instance.ReleaseId((uint)item.Id);
                return false;
            }

            Owner.SendPacket(new SCItemTaskSuccessPacket(reason, tasks, new List<ulong>()));
            return true;
        }
       
        public bool AddExistingItem(Item item, ItemTaskType taskType)
        {
            var tasks = new List<ItemTask>();
            bool successfullyAddedItem;
            //Check if can be stacked on another existing item stack
            if (!Items.Contains(item))
            {
                foreach (var existingItem in Items)
                {
                    if (existingItem != null && existingItem.Template.Id == item.Template.Id && existingItem.Template.MaxCount >= existingItem.Count + item.Count)
                    {
                        //Can stack entire item on existing item
                        existingItem.Count += item.Count;

                        tasks.Add(new ItemCountUpdate(existingItem, item.Count));
                        ItemIdManager.Instance.ReleaseId((uint)item.Id);
                        lock (_removedItems)
                        {
                            if (!_removedItems.Contains(item.Id))
                                _removedItems.Add(item.Id);
                        }
                        successfullyAddedItem = true;
                        break;
                    }
                    else if (existingItem != null && existingItem.Template.Id == item.Template.Id && existingItem.Template.MaxCount < existingItem.Count)
                    {
                        //Can stack part of item on existing item
                        var countAdded = existingItem.Template.MaxCount - existingItem.Count;
                        existingItem.Count += countAdded;
                        item.Count -= countAdded;
                        tasks.Add(new ItemCountUpdate(existingItem, countAdded));
                    }
                }
            }

            //If item still has count >0, move it to first free slot
            var firstFreeSlot = GetFirstFreeSlot(SlotType.Inventory);
            if (firstFreeSlot != -1)
            {
                if (item.Template.LootQuestId > 0)
                {
                    Owner.Quests.OnItemGather(item, item.Count);
                }
                item.Slot = firstFreeSlot;
                item.SlotType = SlotType.Inventory;
                Items[item.Slot] = item;
                tasks.Add(new ItemAdd(item));
                successfullyAddedItem = true;
            }
            else
            {
                //no free slots
                ItemIdManager.Instance.ReleaseId((uint)item.Id);
                lock (_removedItems)
                {
                    if (!_removedItems.Contains(item.Id))
                        _removedItems.Add(item.Id);
                }

                if (!Items.Contains(item))
                    Owner.SendErrorMessage(ErrorMessageType.BagFull);
                successfullyAddedItem = false;
            }

            Owner.SendPacket(new SCItemTaskSuccessPacket(taskType, tasks, new List<ulong>()));
            return successfullyAddedItem;
        }

        public void AddNewItem(uint itemTemplateId, int count, byte grade, ItemTaskType taskType, int toSlot = -1, SlotType type = SlotType.Inventory)
        {
            var tasks = new List<ItemTask>();

            //Check if can be stacked on another existing item stack
            var checkStackable = Items;
            if (type == SlotType.Bank)
                checkStackable = Bank;

            if (toSlot == -1)
            {
                foreach (var existingItem in checkStackable)
                {
                    if (existingItem != null && existingItem.Template.Id == itemTemplateId && existingItem.Template.MaxCount > existingItem.Count)
                    {
                        var countToAdd = Math.Min(count, existingItem.Template.MaxCount - existingItem.Count);
                        existingItem.Count += countToAdd;
                        tasks.Add(new ItemCountUpdate(existingItem, countToAdd));
                        count -= countToAdd;
                    }
                }
            }

            //If count still has count >0, create new item stacks until count == 0 or out of inventory space
            var itemTemplate = ItemManager.Instance.GetTemplate(itemTemplateId);
            while (count > 0 && CountFreeSlots(type) > 0)
            {
                var itemCount = Math.Min(itemTemplate.MaxCount, count);
                var createdItem = ItemManager.Instance.Create(itemTemplateId, itemCount, grade);
                int firstFreeSlot;

                if (toSlot <= -1)
                {
                    firstFreeSlot = GetFirstFreeSlot(type);
                }
                else
                {
                    firstFreeSlot = toSlot;
                }

                count -= itemCount;

                if (firstFreeSlot != -1)
                {
                    if (createdItem.Template.LootQuestId > 0)
                    {
                        Owner.Quests.OnItemGather(createdItem, createdItem.Count);
                    }
                    createdItem.Slot = firstFreeSlot;
                    createdItem.SlotType = type;

                    if (type == SlotType.Inventory)
                    {
                        Items[createdItem.Slot] = createdItem;
                    }
                    else if (type == SlotType.Bank)
                    {
                        Bank[createdItem.Slot] = createdItem;
                    }

                    tasks.Add(new ItemAdd(createdItem));
                }
                else
                {
                    //no free slots, return null
                    ItemIdManager.Instance.ReleaseId((uint)createdItem.Id);
                    lock (_removedItems)
                    {
                        if (!_removedItems.Contains(createdItem.Id))
                            _removedItems.Add(createdItem.Id);
                    }
                    _log.Warn($"Not enough space to add remaining {count} items of itemTemplateId {itemTemplateId}.");
                    break;
                }
            }

            Owner.SendPacket(new SCItemTaskSuccessPacket(taskType, tasks, new List<ulong>()));
            return;

        }

        public List<ItemTask> TryAddItem(Item item, SlotType type, bool dontAddPartialCount)
        {
            if (item == null)
                return null;

            var count = item.Count;
            var tasks = new List<ItemTask>();

            var checkStackable = Items; //Buffer to make sure all items fit before moving if required
            switch (type)
            {
                case SlotType.Bank:
                    checkStackable = Bank;
                    break;
                case SlotType.Mail: //TODO
                case SlotType.Trade:
                    return null;    //TODO
                case SlotType.None:
                    break;
                case SlotType.Equipment:
                    break;
                case SlotType.Inventory:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (item.Template.MaxCount > 1)
            {
                for (var i = 0; i < checkStackable.Length && count > 0; i++)
                {
                    var invItem = checkStackable[i];
                    if (item.CanStackInto(invItem))
                    {
                        var maxChange = Math.Min(invItem.Template.MaxCount - invItem.Count, count);
                        tasks.Add(new ItemCountUpdate(invItem, maxChange));
                        invItem.Count += maxChange;
                        count -= maxChange;
                    }
                }
            }

            if (count > 0)
            {
                var firstFreeSlot = GetFirstFreeSlot(type);
                if (firstFreeSlot != -1)
                {
                    item.Slot = firstFreeSlot;
                    item.SlotType = type;
                    item.Count = count;
                    checkStackable[firstFreeSlot] = item;
                    tasks.Add(new ItemAdd(item));
                    count = 0;
                }
                else if (dontAddPartialCount)
                {
                    return null;
                }
            }

            if (item.Template.LootQuestId > 0)
                Owner.Quests.OnItemGather(item, item.Count);

            if (type == SlotType.Inventory)
                Items = checkStackable;
            else if (type == SlotType.Bank)
                Bank = checkStackable;
            /*else if (type == SlotType.Mail)
                return null; //TODO
            else if (type == SlotType.Trade)
                return null; //TODO */

            return tasks;
        }

        public Item AddItem(Item item)
        {
            if (item.Slot == -1)
            {
                var fItemIndex = -1;
                for (var i = 0; i < Items.Length; i++)
                    if (Items[i]?.Template != null && Items[i].Template.Id == item.Template.Id &&
                        Items[i].Template.MaxCount >= Items[i].Count + item.Count)
                    {
                        fItemIndex = i;
                        break;
                    }

                if (fItemIndex == -1)
                    item.Slot = _freeSlot;
                else
                {
                    var fItem = Items[fItemIndex];
                    fItem.Count += item.Count;
                    ItemIdManager.Instance.ReleaseId((uint)item.Id);
                    
                    if (item.Template.LootQuestId > 0)
                        Owner.Quests.OnItemGather(item, item.Count);
                    
                    return fItem;
                }
            }

            if (item.Slot == -1 && _freeSlot == -1)
                return null;

            if (Items[item.Slot] != null)
                return null;

            item.SlotType = SlotType.Inventory;
            Items[item.Slot] = item;

            _freeSlot = CheckFreeSlot(SlotType.Inventory);

            if (item.Template.LootQuestId > 0)
                Owner.Quests.OnItemGather(item, item.Count);

            return item;

        }

        public void RemoveItem(Item item, bool release, ItemTaskType taskType)
        {
            if (item == null)
                return;

            var tasks = new List<ItemTask>();
            switch (item.SlotType)
            {
                case SlotType.Equipment:
                    Equip[item.Slot] = null;
                    break;
                case SlotType.Inventory:
                    Items[item.Slot] = null;
                    break;
                case SlotType.Bank:
                    Bank[item.Slot] = null;
                    break;
                case SlotType.None:
                    break;
                case SlotType.Trade:
                    break;
                case SlotType.Mail:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            tasks.Add(new ItemRemove(item));
            if (release)
                ItemIdManager.Instance.ReleaseId((uint)item.Id);
            lock (_removedItems)
            {
                if (!_removedItems.Contains(item.Id))
                    _removedItems.Add(item.Id);
            }

            //TODO: Call with ItemTaskType
            Owner.SendPacket(new SCItemTaskSuccessPacket(taskType, tasks, new List<ulong>()));
        }
        public void RemoveItem(Item item, int count, ItemTaskType taskType)
        {
            if (item == null)
                return;

            var tasks = new List<ItemTask>();

            var itemCount = item.Count;
            var temp = Math.Min(count, itemCount);
            item.Count -= temp;
            count -= temp;
            if (count < 0)
                count = 0;

            if (item.Count == 0)
            {
                Items[item.Slot] = null;
                ItemIdManager.Instance.ReleaseId((uint)item.Id);
                lock (_removedItems)
                {
                    if (!_removedItems.Contains(item.Id))
                        _removedItems.Add(item.Id);
                }
                tasks.Add(new ItemRemove(item));
            }
            else
                tasks.Add(new ItemCountUpdate(item, -temp));

            if (count > 0)
                RemoveItem(item.TemplateId, count, taskType, tasks);
            else
                Owner.SendPacket(new SCItemTaskSuccessPacket(taskType, tasks, new List<ulong>()));
        }

        public void RemoveItem(uint templateId, int count, ItemTaskType taskType, List<ItemTask> tasks = null)
        {
            if (tasks == null)
                tasks = new List<ItemTask>();

            foreach (var item in Items)
            {
                if (item == null || item.TemplateId != templateId)
                    continue;

                var itemCount = item.Count;
                var temp = Math.Min(count, itemCount);
                item.Count -= temp;
                count -= temp;
                if (count < 0)
                    count = 0;
                if (item.Count == 0)
                {
                    Items[item.Slot] = null;
                    ItemIdManager.Instance.ReleaseId((uint)item.Id);
                    lock (_removedItems)
                    {
                        if (!_removedItems.Contains(item.Id))
                            _removedItems.Add(item.Id);
                    }
                    tasks.Add(new ItemRemove(item));
                }
                else
                    tasks.Add(new ItemCountUpdate(item, -temp));

                if (count == 0)
                    break;
            }
            Owner.SendPacket(new SCItemTaskSuccessPacket(taskType, tasks, new List<ulong>()));
        }

        public void RemoveItem(uint templateId, int count, ItemTaskType taskType)
        {
            var tasks = new List<ItemTask>();
            foreach (var item in Items)
            {
                if (item == null || item.TemplateId != templateId)
                    continue;

                var itemCount = item.Count;
                var temp = Math.Min(count, itemCount);
                item.Count -= temp;
                count -= temp;
                if (count < 0)
                    count = 0;
                if (item.Count == 0)
                {
                    Items[item.Slot] = null;
                    ItemIdManager.Instance.ReleaseId((uint)item.Id);
                    lock (_removedItems)
                    {
                        if (!_removedItems.Contains(item.Id))
                            _removedItems.Add(item.Id);
                    }
                    tasks.Add(new ItemRemove(item));
                }
                else
                {
                    tasks.Add(new ItemCountUpdate(item, -temp));
                }
                if (count == 0)
                    break;
            }
            Owner.SendPacket(new SCItemTaskSuccessPacket(taskType, tasks, new List<ulong>()));
        }
        
        public void RemoveItem(Item item, bool release)
        {
            switch (item.SlotType)
            {
                case SlotType.Equipment:
                    Equip[item.Slot] = null;
                    break;
                case SlotType.Inventory:
                {
                    Items[item.Slot] = null;
                    if (_freeSlot == -1 || item.Slot < _freeSlot)
                        _freeSlot = item.Slot;
                    break;
                }
                case SlotType.Bank:
                {
                    Bank[item.Slot] = null;
                    if (_freeBankSlot == -1 || item.Slot < _freeBankSlot)
                        _freeBankSlot = item.Slot;
                    break;
                }
                case SlotType.None:
                    break;
                case SlotType.Trade:
                    break;
                case SlotType.Mail:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (release)
                ItemIdManager.Instance.ReleaseId((uint)item.Id);

            lock (_removedItems)
            {
                if (!_removedItems.Contains(item.Id))
                    _removedItems.Add(item.Id);
            }
        }
        
        public List<(Item Item, int Count)> RemoveItem(uint templateId, int count)
        {
            var res = new List<(Item, int)>();
            foreach (var item in Items)
                if (item != null && item.TemplateId == templateId)
                {
                    var itemCount = item.Count;
                    var temp = Math.Min(count, itemCount);
                    item.Count -= temp;
                    count -= temp;
                    if (count < 0)
                        count = 0;
                    if (item.Count == 0)
                    {
                        Items[item.Slot] = null;
                        if (_freeSlot == -1 || item.Slot < _freeSlot)
                            _freeSlot = item.Slot;
                        ItemIdManager.Instance.ReleaseId((uint)item.Id);
                        lock (_removedItems)
                        {
                            if (!_removedItems.Contains(item.Id))
                                _removedItems.Add(item.Id);
                        }
                    }

                    res.Add((item, itemCount - item.Count));
                    if (count == 0)
                        break;
                }

            return res;
        }

        public bool CheckItems(uint templateId, int count) => CheckItems(SlotType.Inventory, templateId, count);

        public bool CheckItems(SlotType slotType, uint templateId, int count)
        {
            Item[] items = null;
            if (slotType == SlotType.Inventory)
                items = Items;
            else if (slotType == SlotType.Equipment)
                items = Equip;
            else if (slotType == SlotType.Bank)
                items = Bank;

            if (items == null)
                return false;

            foreach (var item in items)
                if (item != null && item.TemplateId == templateId)
                {
                    count -= item.Count;
                    if (count < 0)
                        count = 0;
                    if (count == 0)
                        break;
                }

            return count == 0;
        }

        public int GetItemsCount(uint templateId)
        {
            var count = 0;
            foreach (var item in Items)
                if (item != null && item.TemplateId == templateId)
                    count += item.Count;
            return count;
        }

        //For removing item count from an item and moving that count to either a new item or adding to an existing item's count.
        public void SplitItemStack(ulong fromItemId, SlotType fromType, byte fromSlot, ulong toItemId, SlotType toType, byte toSlot, int count = 0)
        {
            var fromItem = GetItem(fromType, fromSlot);
            var toItem = GetItem(toType, toSlot);

            //Make sure you cannot stack different item types, different grades, or items with different flags (eg. bound items into unbound) onto each other.
            if (fromItem == null || (toItem != null && ((fromItem.TemplateId != toItem.TemplateId) || (fromItem.Grade != toItem.Grade) || (fromItem.Flags != toItem.Flags))))
                return;

            if (count >= fromItem.Count)
                count = fromItem.Count;

            if (toItem == null)
            {
                fromItem.Count -= count;
                AddNewItem(fromItem.TemplateId, count, fromItem.Grade, ItemTaskType.Split, toSlot, toType);
                Owner.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.Split, new List<ItemTask>() { new ItemCountUpdate(fromItem, -count) }, new List<ulong>()));
            }
            else
            {
                StackIntoItem(fromItem, toItem, ItemTaskType.Split, count);
            }
        }

        //For moving item count from one item to another.
        public void StackIntoItem(Item fromItem, Item stackIntoItem, ItemTaskType taskType, int count = 0)
        {
            if (count == 0)
                count = Math.Min(fromItem.Count, stackIntoItem.Template.MaxCount - stackIntoItem.Count);

            stackIntoItem.Count += count;
            fromItem.Count -= count;

            var tasks = new List<ItemTask>();

            if (fromItem.Count > 0)
                tasks.Add(new ItemCountUpdate(fromItem, -count));
            else
                RemoveItem(fromItem, true, taskType);

            tasks.Add(new ItemCountUpdate(stackIntoItem, count));
            Owner.SendPacket(new SCItemTaskSuccessPacket(taskType, tasks, new List<ulong>()));
        }

        public void Move(ulong fromItemId, SlotType fromType, byte fromSlot, ulong toItemId, SlotType toType, byte toSlot)
        {
            //TODO: refactor this entire method because it is aids    

            var fromItem = GetItem(fromType, fromSlot);
            var toItem = GetItem(toType, toSlot);

            if (fromItem != null && fromItem.Id != fromItemId)
            {
                _log.Warn("ItemMove: {0} {1}", fromItem.Id, fromItemId);
                // TODO ... ItemNotify?
                return;
            }

            if (toItem != null && toItem.Id != toItemId)
            {
                _log.Warn("ItemMove: {0} {1}", toItem.Id, toItemId);
                // TODO ... ItemNotify?
                return;
            }

            //Check if items can be stacked, if they can't then continue swapping
            if (fromItem != null && toItem != null && fromItem.CanStackInto(toItem))
            {
                StackIntoItem(fromItem, toItem, ItemTaskType.SwapItems);
                return;
            }

            var removingItems = new List<ulong>();
            var tasks = new List<ItemTask>();

            if (fromType == SlotType.Equipment && toItem != null && toItem.Template.BindType == ItemBindType.SoulboundEquip)
            {
                toItem.SetFlag(ItemFlag.SoulBound);
            }

            tasks.Add(new ItemMove(fromType, fromSlot, fromItemId, toType, toSlot, toItemId));

            uint toItemEquipBuffId = 0;
            if (toItem != null)
            {
                toItemEquipBuffId = ItemManager.Instance.GetItemEquipBuff(toItem.TemplateId, toItem.Grade);
            }

            uint fromItemEquipBuffId = 0;
            if (fromItem != null)
            {
                fromItemEquipBuffId = ItemManager.Instance.GetItemEquipBuff(fromItem.TemplateId, fromItem.Grade);
            }

            var updatedEquipment = false;
            var taskOverridden = false;
            if (fromType == SlotType.Equipment)
            {
                if (toItem != null && (fromSlot == (int)EquipmentItemSlot.Mainhand || fromSlot == (int)EquipmentItemSlot.Offhand))
                {
                    var toItemTemplate = ((WeaponTemplate)toItem.Template);
                    var toItemType = toItemTemplate.HoldableTemplate.SlotTypeId;

                    if (fromSlot == (int)EquipmentItemSlot.Mainhand && toItemType == (int)EquipmentItemSlotType.TwoHanded)
                    {
                        //Equipping 2h weapon, remove offhand
                        if (Equip[(int)EquipmentItemSlot.Mainhand] != null && Equip[(int)EquipmentItemSlot.Offhand] != null)
                        {
                            var task = TryRemoveWeapon((int)EquipmentItemSlot.Offhand, toSlot);
                            if (task != null)
                            {
                                Equip[(int)EquipmentItemSlot.Mainhand] = toItem;
                                Equip[(int)EquipmentItemSlot.Mainhand].Slot = (int)EquipmentItemSlot.Mainhand;
                                Equip[(int)EquipmentItemSlot.Mainhand].SlotType = SlotType.Equipment;
                                tasks.Add(task);
                            }
                            else
                            {
                                Owner.SendErrorMessage(ErrorMessageType.CantChangeEquip);
                                return;
                            }
                        }
                        else if (Equip[(int)EquipmentItemSlot.Mainhand] == null && Equip[(int)EquipmentItemSlot.Offhand] != null)
                        {
                            //No equipped main-hand, move offhand to 2h inv slot after 2h equipped
                            Equip[(int)EquipmentItemSlot.Mainhand] = toItem;
                            Items[toSlot] = null;
                            taskOverridden = true;
                            tasks.Add(MoveEquipToEmptyInv((int)EquipmentItemSlot.Offhand, toSlot));
                            taskOverridden = true;
                        }
                        else
                        {
                            Equip[fromSlot] = toItem;
                        }
                        updatedEquipment = true;
                    }
                    else if (fromSlot == (int)EquipmentItemSlot.Offhand && Equip[(int)EquipmentItemSlot.Mainhand] != null && ((WeaponTemplate)Equip[(int)EquipmentItemSlot.Mainhand].Template).HoldableTemplate.SlotTypeId == (int)EquipmentItemSlotType.TwoHanded)
                    {
                        //Equipping an off-hand, remove 2h
                        if (Equip[(int)EquipmentItemSlot.Offhand] != null)
                        {
                            var task = TryRemoveWeapon((int)EquipmentItemSlot.Mainhand, toSlot);
                            if (task != null)
                            {
                                Equip[(int)EquipmentItemSlot.Offhand] = toItem;
                                Equip[(int)EquipmentItemSlot.Offhand].Slot = (int)EquipmentItemSlot.Offhand;
                                Equip[(int)EquipmentItemSlot.Offhand].SlotType = SlotType.Equipment;
                                tasks.Add(task);
                            }
                            else
                            {
                                Owner.SendErrorMessage(ErrorMessageType.CantChangeEquip);
                                return;
                            }
                        }
                        else
                        {
                            //No equipped offhand, move equipped 2h to offhand's inv slot
                            Equip[(int)EquipmentItemSlot.Offhand] = toItem;
                            Items[toSlot] = null;
                            taskOverridden = true;
                            tasks.Add(MoveEquipToEmptyInv((int)EquipmentItemSlot.Mainhand, toSlot));
                        }
                    }
                    else
                    {
                        //Not equipping 2h, or offhand while no 2h equipped
                        Equip[fromSlot] = toItem;
                    }
                }
                else
                {
                    // not equipping mainhand or offhand equipment
                    Equip[fromSlot] = toItem;
                }
                updatedEquipment = true;
            }
            else if (fromType == SlotType.Inventory)
            {
                Items[fromSlot] = toItem;
                if (toItemEquipBuffId != 0 && toType == SlotType.Equipment)
                {
                    Owner.Effects.RemoveBuff(toItemEquipBuffId);
                }
            }
            else if (fromType == SlotType.Bank)
            {
                Bank[fromSlot] = toItem;
                if (toItemEquipBuffId != 0 && toType == SlotType.Equipment)
                {
                    Owner.Effects.RemoveBuff(toItemEquipBuffId);
                }
            }

            if (taskOverridden == false && toType == SlotType.Equipment)
            {
                if (fromItem != null && (toSlot == (int)EquipmentItemSlot.Mainhand || toSlot == (int)EquipmentItemSlot.Offhand))
                {
                    var fromItemTemplate = ((WeaponTemplate)fromItem.Template);
                    var fromItemType = fromItemTemplate.HoldableTemplate.SlotTypeId;

                    if (toSlot == (int)EquipmentItemSlot.Mainhand && fromItemType == (int)EquipmentItemSlotType.TwoHanded)
                    {
                        //Equipping 2h weapon, remove offhand
                        if (Equip[(int)EquipmentItemSlot.Mainhand] != null && Equip[(int)EquipmentItemSlot.Offhand] != null)
                        {
                            var task = TryRemoveWeapon((int)EquipmentItemSlot.Offhand, fromSlot);
                            if (task != null)
                            {
                                Equip[(int)EquipmentItemSlot.Mainhand] = fromItem;
                                Equip[(int)EquipmentItemSlot.Mainhand].Slot = (int)EquipmentItemSlot.Mainhand;
                                Equip[(int)EquipmentItemSlot.Mainhand].SlotType = SlotType.Equipment;
                                tasks.Add(task);
                            }
                            else
                            {
                                Owner.SendErrorMessage(ErrorMessageType.CantChangeEquip);
                                return;
                            }
                        }
                        else if (Equip[(int)EquipmentItemSlot.Mainhand] == null && Equip[(int)EquipmentItemSlot.Offhand] != null)
                        {
                            //No equipped main-hand, move offhand to 2h inv slot after 2h equipped
                            Equip[(int)EquipmentItemSlot.Mainhand] = fromItem; ;
                            Items[fromSlot] = null;
                            taskOverridden = true;
                            tasks.Add(MoveEquipToEmptyInv((int)EquipmentItemSlot.Offhand, fromSlot));
                        }
                        else
                        {
                            Equip[toSlot] = fromItem;
                        }
                        updatedEquipment = true;
                    }
                    else if (toSlot == (int)EquipmentItemSlot.Offhand && Equip[(int)EquipmentItemSlot.Mainhand] != null && ((WeaponTemplate)Equip[(int)EquipmentItemSlot.Mainhand].Template).HoldableTemplate.SlotTypeId == (int)EquipmentItemSlotType.TwoHanded)
                    {
                        //Equipping an off-hand, remove 2h
                        if (Equip[(int)EquipmentItemSlot.Offhand] != null)
                        {
                            var task = TryRemoveWeapon((int)EquipmentItemSlot.Mainhand, fromSlot);
                            if (task != null)
                            {
                                Equip[(int)EquipmentItemSlot.Offhand] = fromItem;
                                Equip[(int)EquipmentItemSlot.Offhand].Slot = (int)EquipmentItemSlot.Offhand;
                                Equip[(int)EquipmentItemSlot.Offhand].SlotType = SlotType.Equipment;
                                tasks.Add(task);
                            }
                            else
                            {
                                Owner.SendErrorMessage(ErrorMessageType.CantChangeEquip);
                                return;
                            }
                        }
                        else
                        {
                            //No equipped offhand, move equipped 2h to offhand's inv slot
                            Equip[(int)EquipmentItemSlot.Offhand] = fromItem; ;
                            Items[fromSlot] = null;
                            taskOverridden = true;
                            tasks.Add(MoveEquipToEmptyInv((int)EquipmentItemSlot.Mainhand, fromSlot));
                        }
                    }
                    else
                    {
                        //Not equipping 2h, or offhand while no 2h equipped
                        Equip[toSlot] = fromItem;
                    }
                }
                else
                {
                    // not equipping mainhand or offhand equipment
                    Equip[toSlot] = fromItem;
                }
                updatedEquipment = true;
            }
            else if (!taskOverridden && toType == SlotType.Inventory)
            {
                Items[toSlot] = fromItem;
                if (fromItemEquipBuffId != 0 && fromType == SlotType.Equipment)
                {
                    Owner.Effects.RemoveBuff(fromItemEquipBuffId);
                }

            }
            else if (toType == SlotType.Bank)
            {
                Bank[toSlot] = fromItem;
                if (fromItemEquipBuffId != 0 && fromType == SlotType.Equipment)
                {
                    Owner.Effects.RemoveBuff(fromItemEquipBuffId);
                }

                if (taskOverridden && fromItem != null)
                {
                    fromItem.Slot = toSlot;
                    fromItem.SlotType = SlotType.Bank;
                }
            }

            if (updatedEquipment == true)
            {
                UpdateEquipmentBuffs();
            }

            if (taskOverridden == false && fromItem != null)
            {
                fromItem.SlotType = toType;
                fromItem.Slot = toSlot;
            }

            if (taskOverridden == false && toItem != null)
            {
                toItem.SlotType = fromType;
                toItem.Slot = fromSlot;
            }

            if (fromItem != null) tasks.Add(new ItemUpdateBits(fromItem));
            if (toItem != null) tasks.Add(new ItemUpdateBits(toItem));

            Owner.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.SwapItems, tasks, removingItems));


            if (fromType == SlotType.Equipment)
                Owner.BroadcastPacket(
                    new SCUnitEquipmentsChangedPacket(Owner.ObjId, new[]
                    {
                        (fromSlot, Equip[fromSlot])
                    }), false);
            if (toType == SlotType.Equipment)
                Owner.BroadcastPacket(
                    new SCUnitEquipmentsChangedPacket(Owner.ObjId, new[]
                    {
                        (toSlot, Equip[toSlot])
                    }), false);

        }

        public void Move(ulong fromItemId, SlotType fromType, byte fromSlot, ulong toItemId, SlotType toType, byte toSlot, int count = 0)
        {
            if (count < 0)
                count = 0;

            var fromItem = GetItem(fromType, fromSlot);
            var toItem = GetItem(toType, toSlot);

            if (fromItem != null && fromItem.Id != fromItemId)
            {
                _log.Warn("ItemMove: {0} {1}", fromItem.Id, fromItemId);
                // TODO ... ItemNotify?
                return;
            }

            if (toItem != null && toItem.Id != toItemId)
            {
                _log.Warn("ItemMove: {0} {1}", toItem.Id, toItemId);
                // TODO ... ItemNotify?
                return;
            }

            var removingItems = new List<ulong>();
            var tasks = new List<ItemTask>();

            tasks.Add(new ItemMove(fromType, fromSlot, fromItemId, toType, toSlot, toItemId));

            if (fromType == SlotType.Equipment)
                Equip[fromSlot] = toItem;
            else if (fromType == SlotType.Inventory)
                Items[fromSlot] = toItem;
            else if (fromType == SlotType.Bank)
                Bank[fromSlot] = toItem;

            if (toType == SlotType.Equipment)
                Equip[toSlot] = fromItem;
            else if (toType == SlotType.Inventory)
                Items[toSlot] = fromItem;
            else if (toType == SlotType.Bank)
                Bank[toSlot] = fromItem;

            if (fromItem != null)
            {
                fromItem.SlotType = toType;
                fromItem.Slot = toSlot;
            }

            if (toItem != null)
            {
                toItem.SlotType = fromType;
                toItem.Slot = fromSlot;
            }

            _freeSlot = CheckFreeSlot(SlotType.Inventory);
            _freeBankSlot = CheckFreeSlot(SlotType.Bank);

            Owner.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.SwapItems, tasks, removingItems));

            if (fromType == SlotType.Equipment)
                Owner.BroadcastPacket(
                    new SCUnitEquipmentsChangedPacket(Owner.ObjId, new[]
                    {
                        (fromSlot, Equip[fromSlot])
                    }), false);
            if (toType == SlotType.Equipment)
                Owner.BroadcastPacket(
                    new SCUnitEquipmentsChangedPacket(Owner.ObjId, new[]
                    {
                        (toSlot, Equip[toSlot])
                    }), false);
        }

        public ItemMove TryRemoveWeapon(byte equipSlot, int ignoreSlot)
        {
            var openSlot = GetFirstFreeSlot(SlotType.Inventory, ignoreSlot);
            if (openSlot == -1)
            {
                //TODO: Notify not enough inv space for offhand
                _log.Warn("TryRemoveWeapon Failed...");
                return null;
            }

            return MoveEquipToEmptyInv(equipSlot, openSlot);
        }

        public ItemMove MoveEquipToEmptyInv(byte equipSlot, int emptyInvSlot)
        {
            if (Items[emptyInvSlot] != null)
            {
                //Empty slot is not empty
                _log.Warn("MoveEquipToEmptyInv: EmptyInvSlot is not empty...");
                return null;
            }

            ulong fromId;
            if (Equip[equipSlot] == null)
            {
                fromId = 0;
            }
            else
            {
                fromId = Equip[equipSlot].Id;
            }
            ulong toId;
            if (Items[emptyInvSlot] == null)
            {
                toId = 0;
            }
            else
            {
                toId = Items[emptyInvSlot].Id;
            }

            var EquipBuffId = ItemManager.Instance.GetItemEquipBuff(Equip[equipSlot].TemplateId, Equip[equipSlot].Grade);
            Owner.Effects.RemoveBuff(EquipBuffId);

            var task = new ItemMove(SlotType.Equipment, equipSlot, fromId, SlotType.Inventory, (byte)emptyInvSlot, toId);

            Items[emptyInvSlot] = Equip[equipSlot];
            Items[emptyInvSlot].Slot = emptyInvSlot;
            Items[emptyInvSlot].SlotType = SlotType.Inventory;
            Equip[equipSlot] = null;

            Owner.BroadcastPacket(new SCUnitEquipmentsChangedPacket(Owner.ObjId, new[] { (equipSlot, Equip[equipSlot]) }), false);

            return task;
        }

        public void UpdateEquipmentBuffs()
        {
            //Weapon type buff
            uint mainHandType = 0;
            WeaponTemplate mainHandTemplate = null;
            if (Equip[(int)EquipmentItemSlot.Mainhand] != null)
            {
                mainHandTemplate = ((WeaponTemplate)Equip[(int)EquipmentItemSlot.Mainhand].Template);
                mainHandType = mainHandTemplate.HoldableTemplate.SlotTypeId;
            }
            uint offHandType = 0;
            WeaponTemplate offHandTemplate = null;
            if (Equip[(int)EquipmentItemSlot.Offhand] != null)
            {
                offHandTemplate = ((WeaponTemplate)Equip[(int)EquipmentItemSlot.Offhand].Template);
                offHandType = offHandTemplate.HoldableTemplate.SlotTypeId;
            }

            uint newWeaponBuff = 0;
            if (offHandType == (int)EquipmentItemSlotType.Shield)
            {
                newWeaponBuff = (uint)WeaponTypeBuff.Shield;
            }
            else if (mainHandType == (int)EquipmentItemSlotType.TwoHanded)
            { //2h
                newWeaponBuff = (uint)WeaponTypeBuff.TwoHanded;
            }
            else if (mainHandType != 0 && offHandType != 0)
            { //duel wield
                newWeaponBuff = (uint)WeaponTypeBuff.DuelWield;
            }
            else
            {
                newWeaponBuff = (uint)WeaponTypeBuff.None;
            }


            if (Owner.WeaponTypeBuffId != newWeaponBuff)
            {
                Owner.Effects.RemoveBuff(Owner.WeaponTypeBuffId);

                if (newWeaponBuff != 0)
                {
                    Owner.Effects.AddEffect(new Effect(Owner, Owner, SkillCaster.GetByType(SkillCasterType.Item), SkillManager.Instance.GetBuffTemplate(newWeaponBuff), null, DateTime.Now));
                }

                Owner.WeaponTypeBuffId = newWeaponBuff;
            }

            //Weapon set buff
            if (mainHandTemplate != null && offHandTemplate != null && mainHandTemplate.EquipSetId == offHandTemplate.EquipSetId)
            {
                if (Owner.WeaponEquipSetBuffId != mainHandTemplate.EquipSetId)
                { //Don't remove and reapply the same buff
                    var WeaponEquipSet = ItemManager.Instance.GetItemSetBonus(mainHandTemplate.EquipSetId);

                    Owner.Effects.RemoveBuff(Owner.WeaponEquipSetBuffId);

                    if (WeaponEquipSet.SetBonuses[2].BuffId != 0 && !Owner.Effects.CheckBuff(WeaponEquipSet.SetBonuses[2].BuffId))
                    {
                        Owner.Effects.AddEffect(new Effect(Owner, Owner, SkillCaster.GetByType(SkillCasterType.Item), SkillManager.Instance.GetBuffTemplate(WeaponEquipSet.SetBonuses[2].BuffId), null, DateTime.Now));
                    }

                    Owner.WeaponEquipSetBuffId = WeaponEquipSet.SetBonuses[2].BuffId;
                }
            }
            else
            {
                Owner.Effects.RemoveBuff(Owner.WeaponEquipSetBuffId);
            }

            //Equip effects
            foreach (var equipItem in Equip)
            {
                if (equipItem != null)
                {
                    var EquipBuffId = ItemManager.Instance.GetItemEquipBuff(equipItem.TemplateId, equipItem.Grade);
                    if (EquipBuffId != 0 && !Owner.Effects.CheckBuff(EquipBuffId))
                    {
                        Owner.Effects.AddEffect(new Effect(Owner, Owner, SkillCaster.GetByType(SkillCasterType.Item), SkillManager.Instance.GetBuffTemplate(EquipBuffId), null, DateTime.Now));
                    }
                }
            }

            //Armor 3/7 set buffs
            //Armor grade buff
            var armor = new List<Armor> {
                (Armor)Equip[(int)EquipmentItemSlot.Head],
                (Armor)Equip[(int)EquipmentItemSlot.Chest],
                (Armor)Equip[(int)EquipmentItemSlot.Waist],
                (Armor)Equip[(int)EquipmentItemSlot.Legs],
                (Armor)Equip[(int)EquipmentItemSlot.Hands],
                (Armor)Equip[(int)EquipmentItemSlot.Feet],
                (Armor)Equip[(int)EquipmentItemSlot.Arms]
            };

            uint maxKind = 0;
            var armorSets = new Dictionary<uint, uint>();
            //Key = Equipment Set Id
            //Value = count

            var armorInfo = new Dictionary<uint, List<uint>>();
            //Key = Armor kind
            //List[0] = count
            //List[1] = lowest grade >= arcane
            //List[2] = calculated ab_level to send

            foreach (var piece in armor)
            {
                if (piece != null)
                {
                    var pieceTemplate = ((ArmorTemplate)piece.Template);

                    var kind = pieceTemplate.KindTemplate.TypeId;
                    var grade = piece.Grade;

                    if (!armorSets.ContainsKey(pieceTemplate.EquipSetId))
                    {
                        armorSets.Add(pieceTemplate.EquipSetId, 1);
                    }
                    else
                    {
                        armorSets[pieceTemplate.EquipSetId]++;
                    }

                    if (!armorInfo.ContainsKey(kind))
                    {
                        armorInfo.Add(kind, new List<uint> { 0, 0, 0 });
                    }
                    armorInfo[kind][0]++;

                    if (grade >= 4)
                    {
                        if (armorInfo[kind][1] == 0)
                        {
                            armorInfo[kind][1] = grade;
                        }
                        else if (armorInfo[kind][1] > grade)
                        {
                            armorInfo[kind][1] = grade;
                        }
                    }

                    if (armorInfo[kind][0] > maxKind)
                    {
                        maxKind = kind;
                    }

                    armorInfo[kind][2] += (uint)((pieceTemplate.Level * pieceTemplate.Level) / 15) + 30;
                }
            }

            uint armorKindBuffId = 0;
            uint armorGradeBuffId = 0;
            if (maxKind != 0 && armorInfo[maxKind][0] >= 4 && armorInfo[maxKind][0] < 7)
            {
                switch (maxKind)
                {
                    case 1:
                        armorKindBuffId = (uint)ArmorKindBuff.Cloth4;
                        break;
                    case 2:
                        armorKindBuffId = (uint)ArmorKindBuff.Leather4;
                        break;
                    case 3:
                        armorKindBuffId = (uint)ArmorKindBuff.Plate4;
                        break;
                }
            }
            else if (maxKind != 0 && armorInfo[maxKind][0] == 7)
            {
                switch (maxKind)
                {
                    case 1:
                        armorKindBuffId = (uint)ArmorKindBuff.Cloth7;
                        break;
                    case 2:
                        armorKindBuffId = (uint)ArmorKindBuff.Leather7;
                        break;
                    case 3:
                        armorKindBuffId = (uint)ArmorKindBuff.Plate7;
                        break;
                }
            }

            if (armorKindBuffId != 0)
            {
                // Half/Full Armor Kind Buff
                if (Owner.ArmorKindBuffId != armorKindBuffId)
                {
                    Owner.Effects.RemoveBuff(Owner.ArmorKindBuffId);
                    Owner.Effects.AddEffect(new Effect(Owner, Owner, SkillCaster.GetByType(SkillCasterType.Item), SkillManager.Instance.GetBuffTemplate(armorKindBuffId), null, DateTime.Now));
                    Owner.ArmorKindBuffId = armorKindBuffId;
                }

                //Armor Grade Buff
                if (Owner.ArmorGradeBuffId != armorGradeBuffId)
                {
                    Owner.Effects.RemoveBuff(Owner.ArmorGradeBuffId);
                }
                armorGradeBuffId = ItemManager.Instance.GetArmorGradeBuffId(maxKind, armorInfo[maxKind][1]);

                if (armorGradeBuffId != 0 && !Owner.Effects.CheckBuff(armorGradeBuffId))
                {
                    Owner.Effects.AddEffect(new Effect(Owner, Owner, SkillCaster.GetByType(SkillCasterType.Item), SkillManager.Instance.GetBuffTemplate(armorGradeBuffId), null, DateTime.Now, (short)armorInfo[maxKind][2]));
                }
                else if (armorGradeBuffId != 0 && Owner.Effects.CheckBuff(armorGradeBuffId))
                {
                    Owner.Effects.AddEffect(new Effect(Owner, Owner, SkillCaster.GetByType(SkillCasterType.Item), SkillManager.Instance.GetBuffTemplate(armorGradeBuffId), null, DateTime.Now, (short)armorInfo[maxKind][2])); //TODO: update buff instead of reapplying
                }

                Owner.ArmorGradeBuffId = armorGradeBuffId;

            }
            else
            {
                Owner.Effects.RemoveBuff(Owner.ArmorKindBuffId);
                Owner.Effects.RemoveBuff(Owner.ArmorGradeBuffId);
                Owner.ArmorKindBuffId = 0;
            }

            //Get Armor Set Bonuses
            var armorSetBuffIds = new List<uint>();
            foreach (var (key, value) in armorSets)
            {
                var armorSet = ItemManager.Instance.GetItemSetBonus(key);
                try
                {
                    if (armorSet.SetBonuses.Keys.Min() <= value)
                    {
                        uint highestNumPieces = 0;
                        foreach (var numPieces in armorSet.SetBonuses.Keys.Where(numPieces => numPieces <= value && numPieces > highestNumPieces))
                        {
                            highestNumPieces = numPieces;
                        }

                        if (highestNumPieces != 0)
                        {
                            armorSetBuffIds.Add(armorSet.SetBonuses[highestNumPieces].BuffId);
                        }
                    }
                }
                catch (ArgumentNullException argumentNullException)
                {
                    // TODO: Handle the System.ArgumentNullException
                }
            }

            //Apply Armor Set Bonuses
            try
            {
                foreach (var buffId in armorSetBuffIds.Where(buffId => !Owner.ArmorSetBuffIds.Contains(buffId)))
                {
                    Owner.Effects.AddEffect(new Effect(Owner, Owner, SkillCaster.GetByType(SkillCasterType.Item), SkillManager.Instance.GetBuffTemplate(buffId), null, DateTime.Now));
                }
            }
            catch (ArgumentNullException argumentNullException)
            {
                // TODO: Handle the System.ArgumentNullException
            }

            //Remove old Armor set bonuses
            try
            {
                foreach (var buffId in Owner.ArmorSetBuffIds.Where(buffId => !armorSetBuffIds.Contains(buffId)))
                {
                    Owner.Effects.RemoveBuff(buffId);
                }
            }
            catch (ArgumentNullException argumentNullException)
            {
                // TODO: Handle the System.ArgumentNullException
            }
            Owner.ArmorSetBuffIds = armorSetBuffIds;
        }

        public bool TakeoffBackpack()
        {
            var backpack = GetItem(SlotType.Equipment, (byte)EquipmentItemSlot.Backpack);
            if (backpack == null) return true;

            // Move to first available slot
            var slot = CheckFreeSlot(SlotType.Inventory);
            if (slot == -1) return false;

            Move(backpack.Id, SlotType.Equipment, (byte)EquipmentItemSlot.Backpack, 0, SlotType.Inventory, (byte)slot, 1);
            return true;
        }

        public Item GetItem(ulong id)
        {
            foreach (var item in Equip)
                if (item != null && item.Id == id)
                    return item;
            foreach (var item in Items)
                if (item != null && item.Id == id)
                    return item;
            return null;
        }

        public Item GetItemByTemplateId(ulong templateId)
        {
            foreach (var item in Equip)
                if (item != null && item.TemplateId == templateId)
                    return item;
            foreach (var item in Items)
                if (item != null && item.TemplateId == templateId)
                    return item;
            return null;
        }

        public Item GetItem(SlotType type, byte slot)
        {
            Item item = null;
            switch (type)
            {
                case SlotType.None:
                    // TODO ...
                    break;
                case SlotType.Equipment:
                    item = Equip[slot];
                    break;
                case SlotType.Inventory:
                    item = Items[slot];
                    break;
                case SlotType.Bank:
                    item = Bank[slot];
                    break;
                case SlotType.Trade:
                    // TODO ...
                    break;
                case SlotType.Mail:
                    // TODO ...
                    break;
            }

            return item;
        }

        public int CountFreeSlots(SlotType type)
        {
            var slot = 0;
            if (type == SlotType.Inventory)
            {
                for (var i = 0; i < Owner.NumInventorySlots; i++)
                    if (Items[i] == null) slot++;
            }
            else if (type == SlotType.Bank)
            {
                for (var i = 0; i < Owner.NumBankSlots; i++)
                    if (Bank[i] == null) slot++;
            }

            return slot;
        }

        public int CheckFreeSlot(SlotType type)
        {
            var slot = 0;
            if (type == SlotType.Inventory)
            {
                while (Items[slot] != null)
                    slot++;
                if (slot > Items.Length)
                    slot = -1;
            }
            else if (type == SlotType.Bank)
            {
                while (Bank[slot] != null)
                    slot++;
                if (slot > Bank.Length)
                    slot = -1;
            }

            return slot;
        }
        

        public int GetFirstFreeSlot(SlotType type)
        {
            var slot = -1;
            if (type == SlotType.Inventory)
            {
                for (var i = 0; i < Owner.NumInventorySlots; i++)
                {
                    if (Items[i] == null)
                    {
                        slot = i;
                        break;
                    }
                }
            }
            else if (type == SlotType.Bank)
            {
                for (var i = 0; i < Owner.NumBankSlots; i++)
                {
                    if (Items[i] == null)
                    {
                        slot = i;
                        break;
                    }
                }
            }

            return slot;
        }

        public int GetFirstFreeSlot(SlotType type, int ignoreSlot)
        {
            var slot = -1;
            if (type == SlotType.Inventory)
            {
                for (var i = 0; i < Owner.NumInventorySlots; i++)
                {
                    if (Items[i] == null && i != ignoreSlot)
                    {
                        slot = i;
                        break;
                    }
                }
            }
            else if (type == SlotType.Bank)
            {
                for (var i = 0; i < Owner.NumBankSlots; i++)
                {
                    if (Items[i] == null && i != ignoreSlot)
                    {
                        slot = i;
                        break;
                    }
                }
            }

            return slot;
        }

        private void SendFragmentedInventory(SlotType slotType, byte numItems, Item[] bag)
        {
            var tempItem = new Item[10];

            if ((numItems % 10) != 0)
                _log.Warn("SendFragmentedInventory: Inventory Size not a multiple of 10 ({0})", numItems);
            if (bag.Length != numItems)
                _log.Warn("SendFragmentedInventory: Inventory Size Mismatch; expected {0} got {1}", numItems, bag.Length);

            for (byte chunk = 0; chunk < (numItems / 10); chunk++)
            {
                Array.Copy(bag, chunk * 10, tempItem, 0, 10);
                Owner.SendPacket(new SCCharacterInvenContentsPacket(slotType, 1, chunk, tempItem));
            }
        }

        public void ExpandSlot(SlotType slotType)
        {
            /*
            * NOTE: server side sqlite db bag_expands table contains no item_id's or  item_counts, old expansion method will always fail
            * while successful expansion is done client side, causing item's placed in the new slots client side to be placed outside of 
            * serverside inventory array.
            */

            if (GetItemsCount(ExpansionScrollId) < 1)
            {
                //Not enough expansion scrolls
                Owner.SendErrorMessage(ErrorMessageType.NotEnoughExpandItem);
                return;
            }

            var slots = 0;
            var price = 0;
            var taskType = ItemTaskType.ExpandBag;
            if (slotType == SlotType.Bank)
            {
                var nextExpand = CharacterManager.Instance.GetExpandForNextStep(SlotType.Bank, ((Owner.NumBankSlots - 50) / 10));
                if (nextExpand != null)
                {
                    if (Owner.Money < nextExpand.Price)
                    {
                        //Not enough money to expand bank
                        Owner.SendErrorMessage(ErrorMessageType.NotEnoughCoin);
                        return;
                    }

                    if (Owner.NumBankSlots >= MaxExpandCapacity)
                    {
                        //Already Expanded bank to maximum size
                        return;
                    }

                    Owner.NumBankSlots += 10;
                    slots = Owner.NumBankSlots;
                    taskType = ItemTaskType.ExpandBank;
                }
                else
                {
                    //Bank already Expanded to maximum size
                    return;
                }
            }

            if (slotType == SlotType.Inventory)
            {
                var nextExpand = CharacterManager.Instance.GetExpandForNextStep(SlotType.Inventory, ((Owner.NumInventorySlots - 50) / 10));
                if (nextExpand != null)
                {
                    if (Owner.Money < nextExpand.Price)
                    {
                        //Not enough money to expand Inventory
                        Owner.SendErrorMessage(ErrorMessageType.NotEnoughCoin);
                        return;
                    }

                    if (Owner.NumBankSlots >= MaxExpandCapacity)
                    {
                        //Already inventory to maximum size
                        return;
                    }

                    Owner.NumInventorySlots += 10;
                    slots = Owner.NumInventorySlots;

                }
                else
                {
                    //Inventory already Expanded to maximum size
                    return;
                }
            }

            if (slotType != SlotType.Inventory && slotType != SlotType.Bank)
            {
                _log.Warn("Invalid slot type tried to be expanded");
                return;
            }

            //Remove 1 expansion scroll
            RemoveItem(ExpansionScrollId, 1, taskType);

            Owner.ChangeMoney(SlotType.Inventory, -price);

            Owner.SendPacket(new SCInvenExpandedPacket(slotType, (byte)slots));
        }

        public void ClearInventory()
        {
            foreach (var item in Items)
            {
                //Remove all non-null and non-teleport book items from inventory
                if (item != null && item.TemplateId != 4045)
                    RemoveItem(item, true, ItemTaskType.Gm);
            }
        }
    }
}
