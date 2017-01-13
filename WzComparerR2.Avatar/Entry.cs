﻿using System;
using System.Collections.Generic;
using System.Text;
using Ookii.Dialogs.Wpf;
using System.Windows.Forms;
using System.Drawing;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.Avatar.UI;
using System.Linq;
using System.IO;

namespace WzComparerR2.Avatar
{
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context)
            : base(context)
        {
        }

        protected override void OnLoad()
        {
            var f = new AvatarForm();
            f.PluginEntry = this;
            var tabCtrl = f.GetTabPanel();
            Context.AddTab(f.Text, tabCtrl);
            Context.SelectedNode1Changed += f.OnSelectedNode1Changed;
            Context.WzClosing += f.OnWzClosing;
            this.Tab = tabCtrl.TabItem;
        }

        public SuperTabItem Tab { get; private set; }
        public void exportChara(bool animated,bool all,object sender, EventArgs e, AvatarCanvas avatar, int bodyFrame, int emoFrame)
        {
            string defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Pictures\\코디";
            Directory.CreateDirectory(defaultDir);

            if (!all)
            {
                //open save dialog
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.AddExtension = true;
                sfd.AutoUpgradeEnabled = true;
                sfd.InitialDirectory = defaultDir;
                sfd.FileName = avatar.ActionName + ".gif";
                sfd.Filter = "GIF 파일|*.gif";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    exportChara_one(animated, avatar, bodyFrame, emoFrame, Path.GetFullPath(sfd.FileName));
                }
            }else{
                VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
                fbd.ShowNewFolderButton = true;
                fbd.SelectedPath = defaultDir;

                if(fbd.ShowDialog() == true) {
                    exportChara_all(avatar, emoFrame, fbd.SelectedPath);
                }
            }
        }
        private void exportChara_all(AvatarCanvas avatar, int emoFrame, string dirPath)
        {
            // init default var
            var faceFrames = avatar.GetFaceFrames(avatar.EmotionName);
            ActionFrame emoF = faceFrames[(emoFrame <= -1 || emoFrame >= faceFrames.Length) ? 0 : emoFrame];

            foreach (var action in avatar.Actions)
            {
                Gif gif = new Gif();

                var actionFrames = avatar.GetActionFrames(action.Name);
                foreach (var frame in actionFrames)
                {
                    // check delay
                    if (frame.Delay != 0)
                    {
                        var bone = avatar.CreateFrame(frame, emoF, null);
                        var bmp = avatar.DrawFrame(bone, frame);

                        Point pos = bmp.OpOrigin;
                        pos.Offset(frame.Flip ? new Point(-frame.Move.X, frame.Move.Y) : frame.Move);
                        GifFrame f = new GifFrame(bmp.Bitmap, new Point(-pos.X, -pos.Y), Math.Abs(frame.Delay));
                        // add frame
                        gif.Frames.Add(f);
                    }
                }

                var gifFile = gif.EncodeGif(Color.Transparent);
                gifFile.Save(dirPath + "\\" + action.Name.Replace('\\', '.') + ".gif");
                gifFile.Dispose();
            }
            // show finished
            MessageBoxEx.Show(dirPath + " 디렉토리에 저장되었습니다.", "알림");
        }
        private void exportChara_one(bool animated, AvatarCanvas avatar, int bodyFrame, int emoFrame,string filePath)
        {
            // get char frames in action
            var actionFrames = avatar.GetActionFrames(avatar.ActionName);
            // get emo frames
            var faceFrames = avatar.GetFaceFrames(avatar.EmotionName);
            // check ani disabled and valid
            animated = (bodyFrame <= -1 || bodyFrame >= actionFrames.Length) || animated;
            ActionFrame emoF = faceFrames[(emoFrame <= -1 || emoFrame >= faceFrames.Length) ? 0 : emoFrame];
            // init gif
            Gif gif = new Gif();
            if (animated)
            {
                // loop
                foreach (var frame in actionFrames)
                {
                    // check delay
                    if (frame.Delay != 0)
                    {
                        var bone = avatar.CreateFrame(frame, emoF, null);
                        var bmp = avatar.DrawFrame(bone, frame);

                        Point pos = bmp.OpOrigin;
                        pos.Offset(frame.Flip ? new Point(-frame.Move.X, frame.Move.Y) : frame.Move);
                        GifFrame f = new GifFrame(bmp.Bitmap, new Point(-pos.X, -pos.Y), Math.Abs(frame.Delay));
                        // add frame
                        gif.Frames.Add(f);
                    }
                } 
            }else{
                var frame = actionFrames[bodyFrame];
                var bone = avatar.CreateFrame(frame, emoF, null);
                var bmp = avatar.DrawFrame(bone, frame);

                Point pos = bmp.OpOrigin;
                pos.Offset(frame.Flip ? new Point(-frame.Move.X, frame.Move.Y) : frame.Move);
                GifFrame f = new GifFrame(bmp.Bitmap, new Point(-pos.X, -pos.Y), Math.Abs(frame.Delay));
                // add frame
                gif.Frames.Add(f);
            }
            var gifFile = gif.EncodeGif(Color.Transparent);
            gifFile.Save(filePath);
            gifFile.Dispose();
            MessageBoxEx.Show(filePath + " 에 저장되었습니다.", "알림");
        }
        public void btnSetting_Click(object sender, EventArgs e)
        {
            AvatarCanvas canvas = new AvatarCanvas();
            canvas.LoadZ();
            canvas.LoadActions();
            canvas.LoadEmotions();

            /*
            cmbAction.Items.Clear();
            foreach (var action in canvas.Actions)
            {
                ComboItem cmbItem = new ComboItem(action.Name);
                switch (action.Level)
                {
                    case 0:
                        cmbItem.FontStyle = System.Drawing.FontStyle.Bold;
                        cmbItem.ForeColor = Color.Indigo;
                        break;

                    case 1:
                        cmbItem.ForeColor = Color.Indigo;
                        break;
                }
                cmbAction.Items.Add(cmbItem);
            }*/

            canvas.ActionName = "stand1";
            canvas.EmotionName = "default";
            canvas.TamingActionName = "stand1";
            AddPart(canvas, "Character\\00002000.img");
            AddPart(canvas, "Character\\00012000.img");
            AddPart(canvas, "Character\\Face\\00020000.img");
            AddPart(canvas, "Character\\Hair\\00030000.img");
            AddPart(canvas, "Character\\Coat\\01040036.img");
            AddPart(canvas, "Character\\Pants\\01060026.img");
            AddPart(canvas, "Character\\Weapon\\01442000.img");
            //AddPart(canvas, "Character\\Weapon\\01382007.img");
            //AddPart(canvas, "Character\\Weapon\\01332000.img");
            //AddPart(canvas, "Character\\Weapon\\01342000.img");

            var faceFrames = canvas.GetFaceFrames(canvas.EmotionName);

            string saveDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\export";
            System.IO.Directory.CreateDirectory(saveDir);

            foreach (var action in canvas.Actions)
            {
                // break;
                Gif gif = new Gif();
                var actionFrames = canvas.GetActionFrames(action.Name);
                foreach (var frame in actionFrames)
                {
                    if (frame.Delay != 0)
                    {
                        var bone = canvas.CreateFrame(frame, faceFrames[0], null);
                        var bmp = canvas.DrawFrame(bone, frame);

                        Point pos = bmp.OpOrigin;
                        pos.Offset(frame.Flip ? new Point(-frame.Move.X, frame.Move.Y) : frame.Move);
                        GifFrame f = new GifFrame(bmp.Bitmap, new Point(-pos.X, -pos.Y), Math.Abs(frame.Delay));
                        gif.Frames.Add(f);
                    }
                }
                

                var gifFile = gif.EncodeGif(Color.Black);

                string fileName = saveDir + "\\" + action.Name.Replace('\\', '.');
                gifFile.Save(fileName + (gif.Frames.Count == 1 ? ".png" : ".gif"));
                gifFile.Dispose();
            }
            /*
            {
                Gif gif = CreateKeyDownAction(canvas);
                var gifFile = gif.EncodeGif(Color.Transparent, 0);
                string fileName = "D:\\d12";

                if (true)
                {
                    var fd = new System.Drawing.Imaging.FrameDimension(gifFile.FrameDimensionsList[0]);
                    //获取帧数(gif图片可能包含多帧，其它格式图片一般仅一帧)
                    int count = gifFile.GetFrameCount(fd);
                    for (int i = 0; i < count; i++)
                    {
                        gifFile.SelectActiveFrame(fd, i);
                        gifFile.Save(fileName + "_" + i + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                gifFile.Save(fileName + (gif.Frames.Count == 1 ? ".png" : ".gif"));
                gifFile.Dispose();
            }
            */
        }

        private Gif CreateContinueAction(AvatarCanvas canvas)
        {
            string afterImage = null;
            Wz_Node defaultAfterImageNode = null;
            if (canvas.Weapon != null)
            {
                afterImage = canvas.Weapon.Node.FindNodeByPath(false, "info", "afterImage").GetValueEx<string>(null);
                if (!string.IsNullOrEmpty(afterImage))
                {
                    defaultAfterImageNode = PluginManager.FindWz("Character\\Afterimage\\" + afterImage + ".img\\10");
                }
            }

            GifCanvas gifCanvas = new GifCanvas();
            gifCanvas.Layers.Add(new GifLayer());
            int delay = 0;
            //foreach (string act in new[] { "alert", "swingP1PoleArm", "doubleSwing", "tripleSwing" })
            //foreach (var act in new object[] { "alert", "swingP1PoleArm", "overSwingDouble", "overSwingTriple" })
            var faceFrames = canvas.GetFaceFrames(canvas.EmotionName);
            //foreach (string act in new[] { "PBwalk1", "PBstand4", "PBstand5" })

            foreach (var act in new object[] {
                
                PluginManager.FindWz("Skill\\2312.img\\skill\\23121004"),
                "stand1",
                PluginManager.FindWz("Skill\\2312.img\\skill\\23121052"),
                //PluginManager.FindWz("Skill\\2112.img\\skill\\21120010"),

                //PluginManager.FindWz("Skill\\200.img\\skill\\2001002"),
                //PluginManager.FindWz("Skill\\230.img\\skill\\2301003"),
                //PluginManager.FindWz("Skill\\230.img\\skill\\2301004"),
                //PluginManager.FindWz("Skill\\231.img\\skill\\2311003"),

                //PluginManager.FindWz("Skill\\13100.img\\skill\\131001010"),
                //"PBwalk1"
            })
            {
                string actionName = null;
                Wz_Node afterImageNode = null;
                List<Gif> effects = new List<Gif>();

                if (act is string)
                {
                    actionName = (string)act;
                }
                else if (act is Wz_Node)
                {
                    Wz_Node skillNode = (Wz_Node)(object)act;
                    actionName = skillNode.FindNodeByPath("action\\0").GetValueEx<string>(null);
                    if (!string.IsNullOrEmpty(afterImage))
                    {
                        afterImageNode = skillNode.FindNodeByPath("afterimage\\" + afterImage);
                    }

                    for (int i = -1; ; i++)
                    {
                        Wz_Node effNode = skillNode.FindNodeByPath("effect" + (i > -1 ? i.ToString() : ""));
                        if (effNode == null)
                            break;
                        effects.Add(Gif.CreateFromNode(effNode, PluginManager.FindWz));
                    }
                }

                if (string.IsNullOrEmpty(actionName))
                {
                    continue;
                }

                //afterImageNode = afterImageNode ?? defaultAfterImageNode;


                //添加特效帧
                foreach (var effGif in effects)
                {
                    if (effGif != null && effGif.Frames.Count > 0)
                    {
                        var layer = new GifLayer();
                        if (delay > 0)
                        {
                            layer.AddBlank(delay);
                        }
                        effGif.Frames.ForEach(af => layer.AddFrame((GifFrame)af));
                        gifCanvas.Layers.Add(layer);
                    }
                }

                //添加角色帧
                ActionFrame[] actionFrames = canvas.GetActionFrames(actionName);
                for (int i = 0; i < actionFrames.Length; i++)
                {
                    var frame = actionFrames[i];

                    if (frame.Delay != 0)
                    {
                        //绘制角色主动作
                        var bone = canvas.CreateFrame(frame, null, null);
                        var bmp = canvas.DrawFrame(bone, frame);
                        GifFrame f = new GifFrame(bmp.Bitmap, bmp.Origin, Math.Abs(frame.Delay));
                        gifCanvas.Layers[0].Frames.Add(f);

                        //寻找刀光帧
                        if (afterImageNode != null)
                        {
                            var afterImageAction = afterImageNode.FindNodeByPath(false, actionName, i.ToString());
                            if (afterImageAction != null)
                            {
                                Gif aGif = Gif.CreateFromNode(afterImageAction, PluginManager.FindWz);
                                if (aGif != null && aGif.Frames.Count > 0) //添加新图层
                                {
                                    var layer = new GifLayer();
                                    if (delay > 0)
                                    {
                                        layer.AddBlank(delay);
                                    }
                                    aGif.Frames.ForEach(af => layer.AddFrame((GifFrame)af));
                                    gifCanvas.Layers.Add(layer);
                                }
                            }
                        }

                        delay += f.Delay;
                    }

                }

            }

            return gifCanvas.Combine();
        }

        private Gif CreateKeyDownAction(AvatarCanvas canvas)
        {
            string afterImage = null;
            Wz_Node defaultAfterImageNode = null;
            if (canvas.Weapon != null)
            {
                afterImage = canvas.Weapon.Node.FindNodeByPath(false, "info", "afterImage").GetValueEx<string>(null);
                if (!string.IsNullOrEmpty(afterImage))
                {
                    defaultAfterImageNode = PluginManager.FindWz("Character\\Afterimage\\" + afterImage + ".img\\10");
                }
            }

            GifCanvas gifCanvas = new GifCanvas();
            var layers = new List<Tuple<GifLayer, int>>();
            var actLayer = new GifLayer();

            //gifCanvas.Layers.Add(new GifLayer());
            int delay = 0;
            var faceFrames = canvas.GetFaceFrames(canvas.EmotionName);

            var skillNode = PluginManager.FindWz("Skill\\2112.img\\skill\\21120018");
            var actionName = skillNode.FindNodeByPath("action\\0").GetValueEx<string>(null);

            int keydownCount = 2;

            foreach (var part in new [] {"prepare", "keydown", "keydownend"})
            {
                var effects = new List<Tuple<Gif,int>>();

                for (int i = -1; ; i++)
                {
                    Wz_Node effNode = skillNode.FindNodeByPath(part + (i > -1 ? i.ToString() : ""));
                    if (effNode == null)
                        break;
                    var gif = Gif.CreateFromNode(effNode, PluginManager.FindWz);
                    var z = effNode.FindNodeByPath("z").GetValueEx(0);
                    effects.Add(new Tuple<Gif, int>(gif, z));
                }

                int effDelay = 0;
                //添加特效帧
                foreach (var effGif in effects)
                {
                    if (effGif.Item1 != null && effGif.Item1.Frames.Count > 0)
                    {
                        var layer = new GifLayer();
                        if (delay > 0)
                        {
                            layer.AddBlank(delay);
                        }

                        int fDelay = 0;

                        for(int i = 0, i0 = part == "keydown" ? keydownCount : 1; i < i0; i++)
                        {
                            effGif.Item1.Frames.ForEach(af => layer.AddFrame((GifFrame)af));
                            layers.Add(new Tuple<GifLayer, int>(layer,effGif.Item2));
                            fDelay+= effGif.Item1.Frames.Select(f => f.Delay).Sum();
                        }

                        effDelay = Math.Max(fDelay, effDelay);
                    }
                }

                delay += effDelay;
            }


            //添加角色帧
            ActionFrame[] actionFrames = canvas.GetActionFrames(actionName);
            int adelay = 0;
            while (adelay < delay)
            {
                for (int i = 0; i < actionFrames.Length; i++)
                {
                    var frame = actionFrames[i];

                    if (frame.Delay != 0)
                    {
                        //绘制角色主动作
                        var bone = canvas.CreateFrame(frame, null, null);
                        var bmp = canvas.DrawFrame(bone, frame);
                        GifFrame f = new GifFrame(bmp.Bitmap, bmp.Origin, Math.Abs(frame.Delay));
                        actLayer.Frames.Add(f);
                        adelay += f.Delay;
                        //delay += f.Delay;
                    }
                }
            }

            layers.Add(new Tuple<GifLayer, int>(actLayer, 0));
            //按照z排序
            layers.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            gifCanvas.Layers.AddRange(layers.Select(t => t.Item1));

            return gifCanvas.Combine();
        }

        void AddPart(AvatarCanvas canvas, string imgPath)
        {
            Wz_Node imgNode = PluginManager.FindWz(imgPath);
            if (imgNode != null)
            {
                canvas.AddPart(imgNode);
            }
        }
    }
}
