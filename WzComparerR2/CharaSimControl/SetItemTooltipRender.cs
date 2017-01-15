using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Resource = CharaSimResource.Resource;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using System.Text.RegularExpressions;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class SetItemTooltipRender : TooltipRender
    {
        public SetItemTooltipRender()
        {
        }

        public SetItem SetItem { get; set; }

        public override object TargetItem
        {
            get { return this.SetItem; }
            set { this.SetItem = value as SetItem; }
        }

        public bool IsCombineProperties { get; set; } = true;

        public override Bitmap Render()
        {
            if (this.SetItem == null)
            {
                return null;
            }

            int picHeight;
            Bitmap originBmp = RenderSetItem(out picHeight);
            Bitmap tooltip = new Bitmap(261, picHeight);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, tooltip.Width, picHeight), GraphicsUnit.Pixel);

            if (originBmp != null)
                originBmp.Dispose();
            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderSetItem(out int picHeight)
        {
            Bitmap setBitmap = new Bitmap(261, DefaultPicHeight);
            Graphics g = Graphics.FromImage(setBitmap);
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;

            picHeight = 10;
            TextRenderer.DrawText(g, this.SetItem.setItemName, GearGraphics.EquipDetailFont2, new Point(261, 10), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
            picHeight += 25;

            format.Alignment = StringAlignment.Far;

            if (this.SetItem.setItemID > 0)
            {
                List<string> partNames = new List<string>();

                foreach (var setItemPart in this.SetItem.itemIDs.Parts)
                {
                    string itemName = setItemPart.Value.RepresentName;
                    string typeName = setItemPart.Value.TypeName;

                    bool Cash = false;
                    BitmapOrigin IconRaw = new BitmapOrigin();

                    foreach (var itemID in setItemPart.Value.ItemIDs)
                    {
                        StringResult sr;
                        if (StringLinker != null)
                        {
                            if (StringLinker.StringEqp.TryGetValue(itemID.Key, out sr))
                            {
                                string[] fullPath = sr.FullPath.Split('\\');
                                Wz_Node itemNode = PluginBase.PluginManager.FindWz(string.Format(@"Character\{0}\{1:D8}.img", String.Join("\\", new List<string>(fullPath).GetRange(2, fullPath.Length - 3).ToArray()), itemID.Key));
                                if (itemNode != null)
                                {
                                    Gear gear = Gear.CreateFromNode(itemNode, PluginManager.FindWz);
                                    Cash = gear.Cash;
                                    IconRaw = gear.IconRaw;
                                }
                            }
                            else if (StringLinker.StringItem.TryGetValue(itemID.Key, out sr))
                            {
                                Wz_Node itemNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\Pet\{0:D7}.img", itemID.Key));
                                if (itemNode != null)
                                {
                                    Item item = Item.CreateFromNode(itemNode, PluginManager.FindWz);
                                    Cash = item.Cash;
                                    IconRaw = item.IconRaw;
                                }
                            }
                        }

                        break;
                    }

                    if (string.IsNullOrEmpty(itemName))
                    {
                        foreach (var itemID in setItemPart.Value.ItemIDs)
                        {
                            StringResult sr = null; ;
                            if (StringLinker != null)
                            {
                                if (StringLinker.StringEqp.TryGetValue(itemID.Key, out sr))
                                {
                                    itemName = sr.Name;
                                }
                                else if (StringLinker.StringItem.TryGetValue(itemID.Key, out sr)) //兼容宠物
                                {
                                    itemName = sr.Name;
                                }
                            }
                            if (sr == null)
                            {
                                itemName = "(null)";
                            }

                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(typeName))
                    {
                        foreach (var itemID in setItemPart.Value.ItemIDs)
                        {
                            StringResult sr = null; ;
                            if (StringLinker != null)
                            {
                                if (StringLinker.StringEqp.TryGetValue(itemID.Key, out sr))
                                {
                                    if (!Cash)
                                        typeName = ItemStringHelper.GetSetItemGearTypeString(Gear.GetGearType(itemID.Key));
                                }
                                else if (StringLinker.StringItem.TryGetValue(itemID.Key, out sr)) //兼容宠物
                                {
                                    if (itemID.Key / 10000 == 500)
                                    {
                                        typeName = "펫";
                                    }
                                    else
                                    {
                                        typeName = "null";
                                    }
                                }
                            }
                            if (sr == null)
                            {
                                typeName = null;
                            }

                            break;
                        }
                    }

                    itemName = itemName ?? string.Empty;
                    typeName = typeName ?? "장비";

                    if (!Regex.IsMatch(typeName, @"^(\(.*\)|（.*）)$"))
                    {
                        typeName = "(" + typeName + ")";
                    }

                    if (!partNames.Contains(itemName + typeName))
                    {
                        partNames.Add(itemName + typeName);
                        Brush brush = setItemPart.Value.Enabled ? Brushes.White : GearGraphics.GrayBrush2;
                        if (!Cash)
                        {
                            TextRenderer.DrawText(g, itemName, GearGraphics.EquipDetailFont2, new Point(8, picHeight), ((SolidBrush)brush).Color);
                            TextRenderer.DrawText(g, typeName, GearGraphics.EquipDetailFont2, new Point(254 - TextRenderer.MeasureText(g, typeName, GearGraphics.EquipDetailFont2).Width, picHeight), ((SolidBrush)brush).Color);
                            picHeight += 18;
                        }
                        else
                        {
                            g.FillRectangle(GearGraphics.GearIconBackBrush2, 10, picHeight, 36, 36);
                            g.DrawImage(Resource.Item_shadow, 10 + 2 + 3, picHeight + 2 + 32 - 6);
                            if (IconRaw.Bitmap != null)
                            {
                                g.DrawImage(IconRaw.Bitmap, 10 + 2 - IconRaw.Origin.X, picHeight + 2 + 32 - IconRaw.Origin.Y);
                            }
                            g.DrawImage(Resource.CashItem_0, 10 + 2 + 20, picHeight + 2 + 32 - 12);
                            TextRenderer.DrawText(g, itemName, GearGraphics.EquipDetailFont2, new Point(50, picHeight), ((SolidBrush)brush).Color);
                            TextRenderer.DrawText(g, typeName, GearGraphics.EquipDetailFont2, new Point(254 - TextRenderer.MeasureText(g, typeName, GearGraphics.EquipDetailFont2).Width, picHeight), ((SolidBrush)brush).Color);
                            picHeight += 40;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.SetItem.completeCount; ++i)
                {
                    TextRenderer.DrawText(g, "(없음)", GearGraphics.EquipDetailFont2, new Point(8, picHeight), ((SolidBrush)GearGraphics.GrayBrush2).Color);
                    TextRenderer.DrawText(g, "미착용", GearGraphics.EquipDetailFont2, new Point(254 - TextRenderer.MeasureText(g, "미착용", GearGraphics.EquipDetailFont2).Width, picHeight), ((SolidBrush)GearGraphics.GrayBrush2).Color);
                    picHeight += 18;
                }
            }

            picHeight += 5;
            g.DrawLine(Pens.White, 6, picHeight, 254, picHeight);//分割线
            picHeight += 9;
            foreach (KeyValuePair<int, SetItemEffect> effect in this.SetItem.effects)
            {
                if (this.SetItem.setItemID > 0)
                    TextRenderer.DrawText(g, effect.Key + "세트효과", GearGraphics.EquipDetailFont, new Point(8, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color);
                else
                    TextRenderer.DrawText(g, "월드 내 중복 착용 효과(" + effect.Key + " / " + this.SetItem.completeCount + ")", GearGraphics.EquipDetailFont, new Point(8, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color);
                picHeight += 15;
                Brush brush = effect.Value.Enabled ? Brushes.White : GearGraphics.GrayBrush2;

                //T116 合并套装
                var props = IsCombineProperties ? CombineProperties(effect.Value.PropsV5) : effect.Value.PropsV5;
                foreach (KeyValuePair<GearPropType, object> prop in props)
                {
                    if (prop.Key == GearPropType.Option)
                    {
                        List<Potential> ops = (List<Potential>)prop.Value;
                        foreach (Potential p in ops)
                        {
                            TextRenderer.DrawText(g, p.ConvertSummary(), GearGraphics.EquipDetailFont2, new Point(8, picHeight), ((SolidBrush)brush).Color);
                            picHeight += 15;
                        }
                    }
                    else if (prop.Key == GearPropType.OptionToMob)
                    {
                        List<SetItemOptionToMob> ops = (List<SetItemOptionToMob>)prop.Value;
                        foreach (SetItemOptionToMob p in ops)
                        {
                            TextRenderer.DrawText(g, p.ConvertSummary(), GearGraphics.EquipDetailFont2, new Point(8, picHeight), ((SolidBrush)brush).Color);
                            picHeight += 15;
                        }
                    }
                    else if (prop.Key == GearPropType.activeSkill)
                    {
                        List<SetItemActiveSkill> ops = (List<SetItemActiveSkill>)prop.Value;
                        foreach (SetItemActiveSkill p in ops)
                        {
                            StringResult sr;
                            if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(p.SkillID, out sr))
                            {
                                sr = new StringResult();
                                sr.Name = p.SkillID.ToString();
                            }
                            string summary = "<" + sr.Name.Replace(Environment.NewLine, "") + "> 스킬 사용 가능";
                            TextRenderer.DrawText(g, summary, GearGraphics.EquipDetailFont2, new Point(8, picHeight), ((SolidBrush)brush).Color);
                            picHeight += 15;
                        }
                    }
                    else
                    {
                        if (ItemStringHelper.GetGearPropString(prop.Key, Convert.ToInt32(prop.Value)) != null)
                        {
                            TextRenderer.DrawText(g, ItemStringHelper.GetGearPropString(prop.Key, Convert.ToInt32(prop.Value)),
                            GearGraphics.SetItemPropFont, new Point(8, picHeight), ((SolidBrush)brush).Color);
                            picHeight += 15;
                        }
                    }
                }
            }
            picHeight += 11;
            format.Dispose();
            g.Dispose();
            return setBitmap;
        }

        private IEnumerable<KeyValuePair<GearPropType, object>> CombineProperties(IEnumerable<KeyValuePair<GearPropType, object>> props)
        {
            var combinedProps = new SortedDictionary<GearPropType, object>();
            var propCache = new SortedDictionary<GearPropType, object>();
            foreach(var kv in props)
            {
                propCache.Add(kv.Key, kv.Value);
            }

            object obj;
            foreach (var prop in propCache)
            {
                switch (prop.Key)
                {
                    case GearPropType.incMHP:
                    case GearPropType.incMMP:
                        if (combinedProps.ContainsKey(GearPropType.incMHP_incMMP))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incMHP? GearPropType.incMMP : GearPropType.incMHP, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incMHP_incMMP, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incMHPr:
                    case GearPropType.incMMPr:
                        if (combinedProps.ContainsKey(GearPropType.incMHPr_incMMPr))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incMHPr ? GearPropType.incMMPr : GearPropType.incMHPr, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incMHPr_incMMPr, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incPAD:
                    case GearPropType.incMAD:
                        if (combinedProps.ContainsKey(GearPropType.incPAD_incMAD))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incPAD ? GearPropType.incMAD : GearPropType.incPAD, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incPAD_incMAD, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incPDD:
                    case GearPropType.incMDD:
                        if (combinedProps.ContainsKey(GearPropType.incPDD_incMDD))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incPDD ? GearPropType.incMDD : GearPropType.incPDD, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incPDD_incMDD, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incACC:
                    case GearPropType.incEVA:
                        if (combinedProps.ContainsKey(GearPropType.incACC_incEVA))
                        {
                            break;
                        }
                        else if (propCache.TryGetValue(prop.Key == GearPropType.incACC ? GearPropType.incEVA : GearPropType.incACC, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incACC_incEVA, prop.Value);
                            break;
                        }
                        goto default;

                    default:
                        combinedProps.Add(prop.Key, prop.Value);
                        break;
                }
            }
            return combinedProps;
        }
    }
}
