using System;
using System.Collections.Generic;
using System.Linq;
using GTANetworkAPI;
using NewProject.Handles;
using NewProject.Core;
using NewProject.SDK;
using NewProject.Chars;
using NewProject.Functions;
using NewProject.Fishing.Models;
using NewProject.Character;
using Newtonsoft.Json;
using NewProject.Chars.Models;

namespace NewProject.Fishing
{
    public interface ITackle
    {
        int Price { get; }
    }
    class FishingTraderProduct : Script
    {
        private static Random random = new();
        private static readonly float[] MultiplierProd = [0.8f, 1.3f];
        private static readonly nLog Log = new nLog("Fishing.FishingTraderProduct");

        public static Dictionary<int, List<TraderItemData>> GetNewProducts()
        {
            try
            {
                Dictionary<int, List<TraderItemData>> finalAssortment = new Dictionary<int, List<TraderItemData>>
                {
                    { (int)ItemId.FishingRod, PickGear(FishingTackle.RodList, random.Next(1, 5)) },
                    { (int)ItemId.FishingReel, PickGear(FishingTackle.ReelList, random.Next(1, 5)) },
                    { (int)ItemId.FishingLine, PickGear(FishingTackle.LineList, random.Next(1, 4)) },
                    { (int)ItemId.FishingFloat, PickGear(FishingTackle.FloatList, random.Next(1, 4)) },
                    { (int)ItemId.FishingLure, PickGear(FishingTackle.LureList, random.Next(1, 4)) },
                    { (int)ItemId.FishingBait, PickGear(FishingTackle.BaitList, random.Next(4, 9)) }
                };

                var selectedFish = FishingManager.FishDataList
                    .OrderBy(x => random.Next())
                    .Take(random.Next(10, 21))
                    .Select(f => CreateShopItem(f.Key, 0, f.Value.Price)).ToList();

                finalAssortment.Add((int)ItemId.Fish, selectedFish);

                return finalAssortment;
            }
            catch (Exception e)
            {
                Log.Write($"GetNewProducts Exception: {e}");
            }
            return null;
        }

        private static List<TraderItemData> PickGear<T>(Dictionary<int, T> source, int count) where T : ITackle
        {
            try
            {
                List<TraderItemData> result = [];

                if (source == null || source.Count == 0) return result;
                var shuffledKeys = source.Keys.OrderBy(x => random.Next()).ToList();

                int maxId = 0;
                foreach (int key in source.Keys)
                {
                    if (key > maxId) maxId = key;
                }
                List<KeyValuePair<int, double>> weightedList = new List<KeyValuePair<int, double>>();
                foreach (int id in source.Keys)
                {
                    double weight = maxId - id + 1;
                    weightedList.Add(new KeyValuePair<int, double>(id, weight * random.NextDouble()));
                }

                weightedList.Sort((x, y) => y.Value.CompareTo(x.Value));

                for (int i = 0; i < count && i < weightedList.Count; i++)
                {
                    int id = weightedList[i].Key;
                    T item = source[id];
                    result.Add(CreateShopItem(id, item.Price, 0));
                }

                return result;
            }
            catch (Exception e)
            {
                Log.Write($"PickGear Exception: {e}");
            }
            return null;
        }

        private static TraderItemData CreateShopItem(int id, int price, int sellPrice)
        {
            try
            {
                float multiplier = (float)(random.NextDouble() * (MultiplierProd[1] - MultiplierProd[0]) + MultiplierProd[0]);

                return new TraderItemData(id, (int)(price * multiplier), (int)(sellPrice * multiplier));
            }
            catch (Exception e)
            {
                Log.Write($"CreateShopItem Exception: {e}");
            }
            return null;

        }
    }
    class FishingTrader
    {
        private static readonly nLog Log = new nLog("Fishing.FishingTrader");



        private static Dictionary<int, TraderData> TradersList = new()
        {
            {0, new TraderData("a_m_m_farmer_01", new Vector3(-51.85, 1908.16, 195.36), 72.9f)},
            {1, new TraderData("a_m_m_hillbilly_02", new Vector3(1734.02, 3030.34, 62.18), 7.72f)},
            {2, new TraderData("a_m_m_hillbilly_01", new Vector3(282.357, 6789.0737, 15.695006), -107.57f)},
        };


        public static void Init()
        {
            try
            {
                foreach (var (id, trader) in TradersList)
                {
                    PedSystem.Repository.CreateQuest(trader.Model, trader.Position, trader.Heading, title: "Скупщик рыбы");
                    trader.ColShape = CustomColShape.CreateCylinderColShape(trader.Position, 2.0f, 2, 0, ColShapeEnums.FishingTrader, Index: id);
                    var newProducts = FishingTraderProduct.GetNewProducts();
                    trader.Products = newProducts;
                    Main.CreateBlip(new Main.BlipData(68, "Скупщик рыбы", trader.Position, 3, true));
                }
            }
            catch (Exception e)
            {
                Log.Write($"Init Exception: {e}");
            }
        }

        public static void SellItems(ExtPlayer player, int trader, int itemId, int id)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (itemId < 1 || id < 1 || characterData == null)
                {
                    Trigger.ClientEvent(player, "client.fishing.closeTrader");
                    return;
                }

                ItemId itemId2 = (ItemId)itemId;
                if (itemId2 != ItemId.Fish || !TradersList.TryGetValue(trader, out var traderData) || !traderData.Products.TryGetValue(itemId, out var productsData))
                {
                    Trigger.ClientEvent(player, "client.fishing.closeTrader");
                    return;
                }

                var productData = productsData.FirstOrDefault(i => i.DataId == id);
                if (productData == null)
                {
                    Trigger.ClientEvent(player, "client.fishing.closeTrader");
                    return;
                }
                string locationChar = $"char_{characterData.UUID}";
                string charLocName = "inventory";

                Chars.Repository.AddInventoryArray(locationChar, charLocName);

                var charInventory = Chars.Repository.ItemsData[locationChar][charLocName];
                float allWeight = 0f;
                foreach (var item in charInventory)
                {
                    if (item.Value.ItemId != ItemId.Fish) continue;

                    int dataId = FishingManager.GetItemData(item.Value.Data, 0);
                    if (dataId != id) continue;
                    float weight = FishingManager.GetFishWeight(item.Value.Data);
                    if (weight > 0) allWeight += weight;

                    ItemId ItemIdDell = item.Value.ItemId;
                    item.Value.ItemId = ItemId.Debug;
                    item.Value.Data = "";
                    Chars.Repository.UpdateSqlItemData(locationChar, charLocName, item.Key, item.Value, ItemIdDell);
                    charInventory.TryRemove(item.Key, out _);

                    Chars.Repository.UpdatePlayerItemData(player, locationChar, charLocName, item.Key, item.Value);
                }

                int price = Convert.ToInt32(productData.SellPrice * allWeight);

                if (price < 1)
                {
                    Trigger.ClientEvent(player, "client.fishing.closeTrader");
                    return;
                }

                MoneySystem.Wallet.Change(player, price);
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы продали рыбу на {MoneySystem.Wallet.Format(price)}$", 3000);

                GameLog.Money($"system", $"player({characterData.UUID})", price, $"fishingTrader(Sell)");

                Trigger.ClientEvent(player, "client.fishing.getTrader", null);
            }
            catch (Exception e)
            {
                Log.Write($"SellItems Exception: {e}");
            }
        }

    }
}