using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Resource = CharaSimResource.Resource;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class ItemTooltipRender2 : TooltipRender
    {
        public ItemTooltipRender2()
        {
        }

        private Item item;

        public Item Item
        {
            get { return item; }
            set { item = value; }
        }

        public override object TargetItem
        {
            get
            {
                return this.item;
            }
            set
            {
                this.item = value as Item;
            }
        }


        public bool LinkRecipeInfo { get; set; }
        public bool LinkRecipeItem { get; set; }
        public bool ShowNickTag { get; set; }

        public TooltipRender LinkRecipeInfoRender { get; set; }
        public TooltipRender LinkRecipeGearRender { get; set; }
        public TooltipRender LinkRecipeItemRender { get; set; }
        public TooltipRender SetItemRender { get; set; }
        public TooltipRender CashPackageRender { get; set; }

        public override Bitmap Render()
        {
            if (this.item == null)
            {
                return null;
            }
            //绘制道具
            int picHeight;
            Bitmap itemBmp = RenderItem(out picHeight);
            Bitmap recipeInfoBmp = null;
            Bitmap recipeItemBmp = null;
            Bitmap setItemBmp = null;

            if (this.item.ItemID / 10000 == 910)
            {
                Wz_Node itemNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\Special\{0:D4}.img\{1}", this.item.ItemID / 10000, this.item.ItemID));
                Wz_Node cashPackageNode = PluginBase.PluginManager.FindWz(string.Format(@"Etc\CashPackage.img\{0}", this.item.ItemID));
                CashPackage cashPackage = CashPackage.CreateFromNode(itemNode, cashPackageNode, PluginManager.FindWz);
                return RenderCashPackage(cashPackage);
            }

            //图纸相关
            int recipeID;
            if (this.item.Specs.TryGetValue(ItemSpecType.recipe, out recipeID))
            {
                int recipeSkillID = recipeID/10000;
                Recipe recipe = null;
                //寻找配方
                Wz_Node recipeNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\Recipe_{0}.img\{1}", recipeSkillID, recipeID));
                if (recipeNode != null)
                {
                    recipe = Recipe.CreateFromNode(recipeNode);
                }
                //生成配方图像
                if (recipe != null)
                {
                    if (this.LinkRecipeInfo)
                    {
                        recipeInfoBmp = RenderLinkRecipeInfo(recipe);
                    }

                    if (this.LinkRecipeItem)
                    {
                        int itemID = recipe.MainTargetItemID;
                        int itemIDClass = itemID / 1000000;
                        if (itemIDClass == 1) //通过ID寻找装备
                        {
                            Wz_Node charaWz = PluginManager.FindWz(Wz_Type.Character);
                            if (charaWz != null)
                            {
                                string imgName = itemID.ToString("d8")+".img";
                                foreach (Wz_Node node0 in charaWz.Nodes)
                                {
                                    Wz_Node imgNode = node0.FindNodeByPath(imgName, true);
                                    if (imgNode != null)
                                    {
                                        Gear gear = Gear.CreateFromNode(imgNode, path=>PluginManager.FindWz(path));
                                        if (gear != null)
                                        {
                                            recipeItemBmp = RenderLinkRecipeGear(gear);
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                        else if (itemIDClass >= 2 && itemIDClass <= 5) //通过ID寻找道具
                        {
                            Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item);
                            if (itemWz != null)
                            {
                                string imgClass = (itemID / 10000).ToString("d4") + ".img\\"+itemID.ToString("d8");
                                foreach (Wz_Node node0 in itemWz.Nodes)
                                {
                                    Wz_Node imgNode = node0.FindNodeByPath(imgClass, true);
                                    if (imgNode != null)
                                    {
                                        Item item = Item.CreateFromNode(imgNode, PluginManager.FindWz);
                                        if (item != null)
                                        {
                                            recipeItemBmp = RenderLinkRecipeItem(item);
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            int setID;
            if (this.item.Props.TryGetValue(ItemPropType.setItemID, out setID))
            {
                SetItem setItem;
                if (CharaSimLoader.LoadedSetItems.TryGetValue(setID, out setItem))
                {
                    setItemBmp = RenderSetItem(setItem);
                }
            }

            //计算布局
            Size totalSize = new Size(itemBmp.Width, picHeight);
            Point recipeInfoOrigin = Point.Empty;
            Point recipeItemOrigin = Point.Empty;
            Point setItemOrigin = Point.Empty;

            if (recipeItemBmp != null)
            {
                recipeItemOrigin.X = totalSize.Width;
                totalSize.Width += recipeItemBmp.Width;

                if (recipeInfoBmp != null)
                {
                    recipeInfoOrigin.X = itemBmp.Width - recipeInfoBmp.Width;
                    recipeInfoOrigin.Y = picHeight;
                    totalSize.Height = Math.Max(picHeight + recipeInfoBmp.Height, recipeItemBmp.Height);
                }
                else
                {
                    totalSize.Height = Math.Max(picHeight, recipeItemBmp.Height);
                }
            }
            else if (recipeInfoBmp != null)
            {
                totalSize.Width += recipeInfoBmp.Width;
                totalSize.Height = Math.Max(picHeight, recipeInfoBmp.Height);
                recipeInfoOrigin.X = itemBmp.Width;
            }
            if (setItemBmp != null)
            {
                setItemOrigin = new Point(totalSize.Width, 0);
                totalSize.Width += setItemBmp.Width;
                totalSize.Height = Math.Max(totalSize.Height, setItemBmp.Height);
            }

            //开始绘制
            Bitmap tooltip = new Bitmap(totalSize.Width, totalSize.Height);
            Graphics g = Graphics.FromImage(tooltip);

            if (itemBmp != null)
            {
                //绘制背景区域
                GearGraphics.DrawNewTooltipBack(g, 0, 0, itemBmp.Width, picHeight);
                //复制图像
                g.DrawImage(itemBmp, 0, 0, new Rectangle(0, 0, itemBmp.Width, picHeight), GraphicsUnit.Pixel);
                //左上角
                g.DrawImage(Resource.UIToolTip_img_Item_Frame2_cover, 3, 3);

                if (this.ShowObjectID)
                {
                    GearGraphics.DrawGearDetailNumber(g, 3, 3, item.ItemID.ToString("d8"), true);
                }
            }

            //绘制配方
            if (recipeInfoBmp != null)
            {
                g.DrawImage(recipeInfoBmp, recipeInfoOrigin.X, recipeInfoOrigin.Y,
                    new Rectangle(Point.Empty, recipeInfoBmp.Size), GraphicsUnit.Pixel);
            }

            //绘制产出道具
            if (recipeItemBmp != null)
            {
                g.DrawImage(recipeItemBmp, recipeItemOrigin.X, recipeItemOrigin.Y,
                    new Rectangle(Point.Empty, recipeItemBmp.Size), GraphicsUnit.Pixel);
            }

            //绘制套装
            if (setItemBmp != null)
            {
                g.DrawImage(setItemBmp, setItemOrigin.X, setItemOrigin.Y,
                    new Rectangle(Point.Empty, setItemBmp.Size), GraphicsUnit.Pixel);
            }

            if (itemBmp != null)
                itemBmp.Dispose();
            if (recipeInfoBmp != null)
                recipeInfoBmp.Dispose();
            if (recipeItemBmp != null)
                recipeItemBmp.Dispose();
            if (setItemBmp != null)
                setItemBmp.Dispose();

            g.Dispose();
            return tooltip;
        }


        private Bitmap RenderItem(out int picH)
        {
            Bitmap tooltip = new Bitmap(290, DefaultPicHeight);
            Graphics g = Graphics.FromImage(tooltip);
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            int value;

            picH = 10;
            //物品标题
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringItem.TryGetValue(item.ItemID, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }

            SizeF titleSize = TextRenderer.MeasureText(g, sr.Name.Replace(Environment.NewLine, ""), GearGraphics.ItemNameFont2, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix);
            titleSize.Width += 12 * 2;
            if (titleSize.Width > 290)
            {
                //重构大小
                g.Dispose();
                tooltip.Dispose();

                tooltip = new Bitmap((int)Math.Ceiling(titleSize.Width), DefaultPicHeight);
                g = Graphics.FromImage(tooltip);
                picH = 10;
            }

            //绘制标题
            format.Alignment = StringAlignment.Center;
            TextRenderer.DrawText(g, sr.Name.Replace(Environment.NewLine, ""), GearGraphics.ItemNameFont2, new Point(tooltip.Width, picH), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            picH += 21;

            bool hasInfo = false;
            //额外特性
            string attr = GetItemAttributeString();
            if (!string.IsNullOrEmpty(attr))
            {
                TextRenderer.DrawText(g, attr, GearGraphics.ItemDetailFont, new Point(tooltip.Width, picH), ((SolidBrush)GearGraphics.GearNameBrushC).Color, TextFormatFlags.HorizontalCenter);
                picH += 16;
                hasInfo = true;
            }

            if (item.TimeLimited)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = time.ToString("yyyy년 M월 d일 HH시 mm분까지 사용가능");
                TextRenderer.DrawText(g, expireStr, GearGraphics.ItemDetailFont, new Point(tooltip.Width, picH), Color.White, TextFormatFlags.HorizontalCenter);
                picH += 16;
                hasInfo = true;
            }
            else if (item.EndUseDate != null)
            {
                string expireStr = string.Format("{0}년 {1}월 {2}일 {3:D2}시 {4:D2}분까지 사용가능", Convert.ToInt32(item.EndUseDate.Substring(0, 4)), Convert.ToInt32(item.EndUseDate.Substring(4, 2)), Convert.ToInt32(item.EndUseDate.Substring(6, 2)), Convert.ToInt32(item.EndUseDate.Substring(8, 2)), Convert.ToInt32(item.EndUseDate.Substring(10, 2)));
                TextRenderer.DrawText(g, expireStr, GearGraphics.ItemDetailFont, new Point(tooltip.Width, picH), Color.White, TextFormatFlags.HorizontalCenter);
                picH += 16;
                hasInfo = true;
            }
            if (item.Props.TryGetValue(ItemPropType.limitedLife, out value) && value != 0)
            {
                string expireStr = string.Format("마법의 시간: {0}시간 {1}분", value / 3600, (value % 3600) / 60);
                TextRenderer.DrawText(g, expireStr, GearGraphics.ItemDetailFont, new Point(tooltip.Width, picH), Color.White, TextFormatFlags.HorizontalCenter);
                picH += 16;
                hasInfo = true;
            }
            else if (item.Props.TryGetValue(ItemPropType.life, out value) && value != 0)
            {
                DateTime time = DateTime.Now.AddDays(value);
                string expireStr = time.ToString("마법의 시간: yyyy년 M월 d일 HH시까지");
                TextRenderer.DrawText(g, expireStr, GearGraphics.ItemDetailFont, new Point(tooltip.Width, picH), Color.White, TextFormatFlags.HorizontalCenter);
                picH += 16;
                hasInfo = true;
            }

            if (hasInfo)
            {
                picH += 4;
            }

            //绘制图标
            int iconY = picH;
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_base, 14, picH);
            if (item.Icon.Bitmap != null)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(item.Icon.Bitmap),
                20 + (1 - item.Icon.Origin.X) * 2,
                picH + 6 + (33 - item.Icon.Bitmap.Height) * 2);
            }
            if (item.Cash)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(Resource.CashItem_0),
                    20 + 68 - 26,
                    picH + 6 + 68 - 26);
            }
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_new, 14 + 7, picH + 7);
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_cover, 18, picH + 4); //绘制左上角cover

            if (item.Props.TryGetValue(ItemPropType.reqLevel, out value))
            {
                picH += 4;
                g.DrawImage(Resource.ToolTip_Equip_Can_reqLEV, 100, picH);
                GearGraphics.DrawGearDetailNumber(g, 150, picH, value.ToString(), true);
                picH += 15;
            }
            else
            {
                picH += 3;
            }

            int right = tooltip.Width - 18;

            string desc = null;
            if (item.Level > 0)
            {
                desc += $"[LV.{item.Level}] ";
            }
            desc += sr.Desc;
            if (!string.IsNullOrEmpty(desc))
            {
                GearGraphics.DrawString(g, desc, GearGraphics.ItemDetailFont2, 100, right, ref picH, 16);
            }
            if (!string.IsNullOrEmpty(sr.AutoDesc))
            {
                GearGraphics.DrawString(g, sr.AutoDesc, GearGraphics.ItemDetailFont2, 100, right, ref picH, 16);
            }
            if (item.Props.TryGetValue(ItemPropType.tradeAvailable, out value) && value > 0)
            {
                attr = ItemStringHelper.GetItemPropString(ItemPropType.tradeAvailable, value);
                if (!string.IsNullOrEmpty(attr))
                    GearGraphics.DrawString(g, "#c" + attr + "#", GearGraphics.ItemDetailFont2, 100, right, ref picH, 16);
            }
            if (item.Specs.TryGetValue(ItemSpecType.recipeValidDay, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "( 제작 가능 기간 : " + value + "일 )", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.Specs.TryGetValue(ItemSpecType.recipeUseCount, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "( 제작 가능 횟수 : " + value + "회 )", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.ItemID / 1000 == 5533)
            {
                GearGraphics.DrawString(g, "\n#c더블 클릭 시 미리보기에서 상자 속 아이템들을 3초마다 차례로 확인할 수 있습니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.Cash)
            {
                if (item.Props.TryGetValue(ItemPropType.noMoveToLocker, out value) && value > 0)
                {
                    GearGraphics.DrawString(g, "\n#c캐시 보관함으로 이동시킬 수 없는 아이템입니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                }
                else
                {
                    GearGraphics.DrawString(g, "\n#c사용 전 1회에 한해 타인과 교환할 수 있으며, 아이템 사용 후에는 교환이 제한됩니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                }
            }
            if (item.Props.TryGetValue(ItemPropType.flatRate, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "\n#c기간 정액제 아이템입니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }

            string incline = null;
            ItemPropType[] inclineTypes = new ItemPropType[]{
                    ItemPropType.charismaEXP,
                    ItemPropType.insightEXP,
                    ItemPropType.willEXP,
                    ItemPropType.craftEXP,
                    ItemPropType.senseEXP,
                    ItemPropType.charmEXP };

            string[] inclineString = new string[]{
                    "카리스마","통찰력","의지","손재주","감성","매력"};

            for (int i = 0; i < inclineTypes.Length; i++)
            {
                if (item.Props.TryGetValue(inclineTypes[i], out value) && value > 0)
                {
                    incline += ", " + inclineString[i] + " " + value;
                }
            }

            if (!string.IsNullOrEmpty(incline))
            {
                GearGraphics.DrawString(g, "#c장착 시 1회에 한해 " + incline.Substring(2) + "의 경험치를 얻으실 수 있습니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }

            picH += 3;

            Wz_Node nickResNode = null;
            bool willDrawNickTag = this.ShowNickTag
                && this.Item.Props.TryGetValue(ItemPropType.nickTag, out value)
                && this.TryGetNickResource(value, out nickResNode);
            int minLev = 0, maxLev = 0;
            bool willDrawExp = item.Props.TryGetValue(ItemPropType.exp_minLev, out minLev) && item.Props.TryGetValue(ItemPropType.exp_maxLev, out maxLev);
            if (item.Sample.Bitmap != null || willDrawNickTag || willDrawExp)
            {
                if (picH < iconY + 84)
                {
                    picH = iconY + 84;
                }
                if (item.Sample.Bitmap != null)
                {
                    g.DrawImage(item.Sample.Bitmap, (tooltip.Width - item.Sample.Bitmap.Width) / 2, picH);
                    picH += item.Sample.Bitmap.Height;
                    picH += 2;
                }
                if (minLev > 0 && maxLev > 0)
                {
                    long[] expTable = {0, 15, 34, 57, 92, 135, 372, 450, 840, 1242,
                        1242, 1242, 1242, 1242, 1242, 1490, 1788, 2145, 2574, 3088,
                        3705, 4446, 5335, 6402, 7682, 9218, 11061, 13273, 15927, 19112,
                        19112, 19112, 19112, 19112, 19112, 22934, 27520, 33024, 39628, 47553,
                        51357, 55465, 59902, 64694, 69869, 75458, 81494, 88013, 95054, 102658,
                        110870, 119739, 129318, 139663, 150836, 162902, 175934, 190008, 205208, 221624,
                        221624, 221624, 221624, 221624, 221624, 238245, 256113, 275321, 295970, 318167,
                        342029, 367681, 395257, 424901, 456768, 488741, 522952, 559558, 598727, 640637,
                        685481, 733464, 784806, 839742, 898523, 961419, 1028718, 1100728, 1177778, 1260222,
                        1342136, 1429374, 1522283, 1621231, 1726611, 1838840, 1958364, 2085657, 2221224, 2365603,
                        2365603, 2365603, 2365603, 2365603, 2365603, 2519367, 2683125, 2857528, 3043267, 3241079,
                        3451749, 3676112, 3915059, 4169537, 4440556, 4729192, 5036589, 5363967, 5712624, 6083944,
                        6479400, 6900561, 7349097, 7826788, 8335529, 8877338, 9454364, 10068897, 10723375, 11420394,
                        12162719, 12953295, 13795259, 14691950, 15646926, 16663976, 17747134, 18900697, 20129242, 21437642,
                        22777494, 24201087, 25713654, 27320757, 29028304, 30842573, 32770233, 34818372, 36994520, 39306677,
                        41763344, 44373553, 47146900, 50093581, 53224429, 56550955, 60085389, 63840725, 67830770, 72070193,
                        76574580, 81360491, 86445521, 91848366, 97588888, 103688193, 110168705, 117054249, 124370139, 132143272,
                        140402226, 149177365, 158500950, 168407259, 178932712, 190116006, 201998256, 214623147, 228037093, 242289411,
                        256826775, 272236381, 288570563, 305884796, 324237883, 343692155, 364313684, 386172505, 409342855, 433903426,
                        459937631, 487533888, 516785921, 547793076, 580660660, 615500299, 652430316, 691576134, 733070702, 777054944,
                        2207026470, 2648431764, 3178118116, 3813741739, 4576490086, 5491788103, 6590145723, 7908174867, 9489809840, 11387771808,
                        24142076232, 25590600805, 27126036853, 28753599064, 30478815007, 32307543907, 34245996541, 36300756333, 38478801712, 40787529814,
                        84838062013, 88231584493, 91760847872, 95431281786, 99248533057, 103218474379, 107347213354, 111641101888, 116106745963, 120751015801,
                        246332072234, 251258713678, 256283887951, 261409565710, 266637757024, 271970512164, 277409922407, 282958120855, 288617283272, 294389628937,
                        594667050452, 600613720956, 606619858165, 612686056746, 618812917313, 625001046486, 631251056950, 637563567519, 643939203194, 650378595225};

                    long totalExp = 0;

                    for (int i = minLev; i < maxLev; i++)
                        totalExp += expTable[i];

                    g.DrawLine(Pens.White, 6, picH, tooltip.Width - 7, picH);
                    picH += 8;

                    TextRenderer.DrawText(g, "총  경험치량 :" + totalExp, GearGraphics.ItemDetailFont2, new Point(10, picH), ((SolidBrush)GearGraphics.GearNameBrushC).Color, TextFormatFlags.NoPadding);
                    picH += 16;

                    TextRenderer.DrawText(g, "잔여 경험치량:" + totalExp, GearGraphics.ItemDetailFont2, new Point(10, picH), Color.Red, TextFormatFlags.NoPadding);
                    picH += 16;
                }
                if (nickResNode != null)
                {
                    this.DrawNickTag(g, nickResNode, sr.Name, tooltip.Width, ref picH);
                    picH += 4;
                }
            }


            //绘制配方需求
            if (item.Specs.TryGetValue(ItemSpecType.recipe, out value))
            {
                int reqSkill, reqSkillLevel;
                if (!item.Specs.TryGetValue(ItemSpecType.reqSkill, out reqSkill))
                {
                    reqSkill = value / 10000 * 10000;
                }

                if (!item.Specs.TryGetValue(ItemSpecType.reqSkillLevel, out reqSkillLevel))
                {
                    reqSkillLevel = 1;
                }

                picH = Math.Max(picH, iconY + 107);
                g.DrawLine(Pens.White, 6, picH, 283, picH);//分割线
                picH += 10;
                TextRenderer.DrawText(g, "< 사용 제한조건 >", GearGraphics.ItemDetailFont, new Point(8, picH), ((SolidBrush)GearGraphics.SetItemNameBrush).Color, TextFormatFlags.NoPadding);
                picH += 17;

                //技能标题
                if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(reqSkill, out sr))
                {
                    sr = new StringResult();
                    sr.Name = "(null)";
                }
                switch (sr.Name)
                {
                    case "장비제작": sr.Name = "장비 제작"; break;
                    case "장신구제작": sr.Name = "장신구 제작"; break;
                }
                TextRenderer.DrawText(g, string.Format("· {0} {1}레벨 이상", sr.Name, reqSkillLevel), GearGraphics.ItemDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.SetItemNameBrush).Color, TextFormatFlags.NoPadding);
                picH += 16;
                picH += 6;
            }

            picH = Math.Max(iconY + 94, picH + 6);
            return tooltip;
        }

        private string GetItemAttributeString()
        {
            int value, value2;
            List<string> tags = new List<string>();

            if (item.Props.TryGetValue(ItemPropType.quest, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.quest, value));
            }
            if (item.Props.TryGetValue(ItemPropType.pquest, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.pquest, value));
            }
            if (item.Props.TryGetValue(ItemPropType.only, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.only, value));
            }
            if (item.Props.TryGetValue(ItemPropType.tradeBlock, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.tradeBlock, value));
            }
            if (item.Props.TryGetValue(ItemPropType.accountSharable, out value) && value != 0)
            {
                if (item.Props.TryGetValue(ItemPropType.exp_minLev, out value2) && value2 != 0)
                {
                    tags.Add("사용시 교환 불가");
                }
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.accountSharable, value));
            }
            if (item.Props.TryGetValue(ItemPropType.multiPet, out value))
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.multiPet, value));
            }
            else if (item.ItemID / 10000 == 500)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.multiPet, 0));
            }

            return tags.Count > 0 ? string.Join(", ", tags.ToArray()) : null;
        }

        private Bitmap RenderLinkRecipeInfo(Recipe recipe)
        {
            TooltipRender renderer = this.LinkRecipeInfoRender;
            if (renderer == null)
            {
                RecipeTooltipRender defaultRenderer = new RecipeTooltipRender();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = recipe;
            return renderer.Render();
        }

        private Bitmap RenderLinkRecipeGear(Gear gear)
        {
            TooltipRender renderer = this.LinkRecipeGearRender;
            if (renderer == null)
            {
                GearTooltipRender2 defaultRenderer = new GearTooltipRender2();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = gear;
            return renderer.Render();
        }

        private Bitmap RenderLinkRecipeItem(Item item)
        {
            TooltipRender renderer = this.LinkRecipeItemRender;
            if (renderer == null)
            {
                ItemTooltipRender2 defaultRenderer = new ItemTooltipRender2();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = item;
            return renderer.Render();
        }

        private Bitmap RenderSetItem(SetItem setItem)
        {
            TooltipRender renderer = this.SetItemRender;
            if (renderer == null)
            {
                var defaultRenderer = new SetItemTooltipRender();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = setItem;
            return renderer.Render();
        }

        private Bitmap RenderCashPackage(CashPackage cashPackage)
        {
            TooltipRender renderer = this.CashPackageRender;
            if (renderer == null)
            {
                var defaultRenderer = new CashPackageTooltipRender();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = this.ShowObjectID;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = cashPackage;
            return renderer.Render();
        }

        private bool TryGetNickResource(int nickTag, out Wz_Node resNode)
        {
            resNode = PluginBase.PluginManager.FindWz("UI/NameTag.img/nick/" + nickTag);
            return resNode != null;
        }

        private void DrawNickTag(Graphics g, Wz_Node resNode, string nickName, int picW, ref int picH)
        {
            if (g == null || resNode == null)
                return;

            //加载资源和文本颜色
            var wce = new[] { "w", "c", "e" }.Select(n =>
            {
                var node = resNode.FindNodeByPath(n);
                if (node == null)
                {
                    return new BitmapOrigin();
                }
                return BitmapOrigin.CreateFromNode(node, PluginBase.PluginManager.FindWz);
            }).ToArray();

            Color color = Color.FromArgb(resNode.FindNodeByPath("clr").GetValueEx(-1));

            //测试y轴大小
            int offsetY = wce.Min(bmp => bmp.OpOrigin.Y);
            int height = wce.Max(bmp => bmp.Rectangle.Bottom);

            //测试宽度
            var font = GearGraphics.ItemDetailFont;
            var fmt = StringFormat.GenericTypographic;
            int width = string.IsNullOrEmpty(nickName) ? 0 : TextRenderer.MeasureText(g, nickName, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            int left = picW / 2 - width / 2;
            int right = left + width;

            //开始绘制背景
            picH -= offsetY;
            if (wce[0].Bitmap != null)
            {
                g.DrawImage(wce[0].Bitmap, left - wce[0].Origin.X, picH - wce[0].Origin.Y);
            }
            if (wce[1].Bitmap != null) //不用拉伸 用纹理平铺 看运气
            {
                var brush = new TextureBrush(wce[1].Bitmap);
                Rectangle rect = new Rectangle(left, picH - wce[1].Origin.Y, right - left, brush.Image.Height);
                brush.TranslateTransform(rect.X, rect.Y);
                g.FillRectangle(brush, rect);
                brush.Dispose();
            }
            if (wce[2].Bitmap != null)
            {
                g.DrawImage(wce[2].Bitmap, right - wce[2].Origin.X, picH - wce[2].Origin.Y);
            }

            //绘制文字
            if (!string.IsNullOrEmpty(nickName))
            {
                var brush = new SolidBrush(color);
                TextRenderer.DrawText(g, nickName, font, new Point(picW / 2 - width / 2, picH), color, TextFormatFlags.NoPadding);
                brush.Dispose();
            }

            picH += height;
        }
    }
}
