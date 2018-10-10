using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DxLibDLL;//DxLibを使用
using CoreTweet; //CoreTweetを使用
using System.IO;//FileInfoを使用
using SpeechLib; //音声認識用

namespace DesctopMascot
{
    public partial class Form1 : Form
    {
        private struct modelMenuData
        {
            public int modelNum;
            public string modelName;
            public string modelPass;
        }
        modelMenuData[] modelMenu;

        private struct musicmMenuData
        {
            public int musicNum;
            public string musicName;
            public string musicPass;
        }
        musicmMenuData[] musicMenu;

        private int modelHandle;
        private int cameraHandle;
        private int soundHandle;
        private int nowModelIndex;
        private int attachIndex;
        private float totalTime;
        private float playTime;
        private const float playSpeed = 0.48f;
        private int motionNum;
        private const int maxNum = 2;
        private const int skipLine = 1;
        private Tokens token;
        private int NowInput, EdgeInput, PrevInput;
        private int Catch;
        private int CatchMouseX, CatchMouseY;
        private DX.VECTOR Catch3DModelPosition;
        private DX.VECTOR Catch3DHitPosition;
        private DX.VECTOR Catch2DHitPosition;
        private DX.VECTOR scale;
        private float rot;
        private int musicIndex;

        private enum ModelNum
        {
            KAEDE,
            RIN
        }

        private enum MusicNum
        {
            KOIKAZE
        }

        private enum MotionNum
        {
            FUYU,
            FUYU2,
            KOIKAZE
        }

        public Form1()
        {
            InitializeComponent();

            MouseWheel += new MouseEventHandler(Form1_MouseWheel);


            ClientSize = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Text = "DesktopMascot";
            AllowDrop = true;       //ドラッグ&ドロップを許可


            /*token = Tokens.Create("YRZGPgGkiWE9uDFyph77AgHQw", "TZhB8DP10P99YzLHlkAQtOT7ofC1fyPd7pgfe05Nk4Nbo5uJHt",
                "110161860-XIPabNbSW4Lg5w6jVpNTwc2MuNECxjolnSR0d6PE", "0ZnG5HsfKiD0oprCHPOEofHd27Jg2MwL58BqbEU0POBX4"); //twitterアカウントの認証*/

            DX.SetOutApplicationLogValidFlag(DX.FALSE);    //Log.txtを生成しないように設定
            DX.SetUserWindow(Handle);                      //DxLibの親ウインドウをこのフォームに設定
            DX.SetZBufferBitDepth(24);                     //Zバッファの精度を変更
            DX.DxLib_Init();                               //DxLibの初期化処理
            DX.SetDrawScreen(DX.DX_SCREEN_BACK);           //描画先を裏画面に設定

            System.Random rand = new System.Random();
            motionNum = rand.Next(maxNum);

            var list = new List<string>();
            list = LoadSettingFile("ModelSetting.csv");
            //最初の一行分は飛ばす
            modelMenu = new modelMenuData[list.Count - skipLine];
            for(int ii = 0; ii < list.Count - 1; ii++)
            {
                //最初の一行は項目名なので飛ばす
                string line = list[ii + 1];
                string[] data = line.Split(',');
                modelMenu[ii].modelNum = int.Parse(data[0]);
                modelMenu[ii].modelName = data[1];
                modelMenu[ii].modelPass = data[2];
            }

            //最初の一行分は飛ばす
            var musiclist = new List<string>();
            musiclist = LoadSettingFile("MusicSetting.csv");
            musicMenu = new musicmMenuData[musiclist.Count - skipLine];
            for(int ii = 0; ii < musiclist.Count - skipLine; ii++)
            {
                string line = musiclist[ii + skipLine];
                string[] data = line.Split(',');
                musicMenu[ii].musicNum = int.Parse(data[0]);
                musicMenu[ii].musicName = data[1];
                musicMenu[ii].musicPass = data[2];
            }

            modelHandle = DX.MV1LoadModel(modelMenu[(int)ModelNum.KAEDE].modelPass);
            cameraHandle = 0;
            nowModelIndex = 0;
            attachIndex = DX.MV1AttachAnim(modelHandle, motionNum, -1, DX.FALSE);
            totalTime = DX.MV1GetAttachAnimTotalTime(modelHandle, attachIndex);
            playTime = 0.0f;
            scale = DX.MV1GetScale(modelHandle);
            NowInput = 0;
            EdgeInput = 0;
            PrevInput = 0;
            Catch = 0;
            musicIndex = 0;
            soundHandle = 0;


            DX.SetCameraNearFar(0.1f, 1000.0f);//奥行0.1～1000をカメラの描画範囲とする
            DX.SetCameraPositionAndTarget_UpVecY(DX.VGet(12.0f, 25.0f, -35.0f), DX.VGet(0.0f, 15.0f, 0.0f));//第1引数の位置から第2引数の位置を見る角度にカメラを設置

            DX.MV1SetupCollInfo(modelHandle, -1, 8, 8, 8);

            ToolStripMenuItem mainMenu = new ToolStripMenuItem();
            mainMenu.Text = "モデルの変更";

            //モデルの選択メニューの作成
            ToolStripMenuItem[] childMenu = new ToolStripMenuItem[modelMenu.Length];
            for (int ii = 0; ii < modelMenu.Length; ii++)
            {
                childMenu[ii] = new ToolStripMenuItem();
                childMenu[ii].Text = modelMenu[ii].modelName;
                childMenu[ii].Click += contextMenuStrip_SubMenuClick;
                mainMenu.DropDownItems.Add(childMenu[ii]);
            }

            ToolStripMenuItem mainMenu2 = new ToolStripMenuItem();
            mainMenu2.Text = "音楽の再生";

            //モデルに躍らせたい楽曲の選択メニュー
            ToolStripMenuItem[] childMenu2 = new ToolStripMenuItem[musicMenu.Length];
            for (int ii = 0; ii < musicMenu.Length; ii++)
            {
                childMenu2[ii] = new ToolStripMenuItem();
                childMenu2[ii].Text = musicMenu[ii].musicName;
                childMenu2[ii].Click += contextMenuStrip_SubMenu2Click;
                mainMenu2.DropDownItems.Add(childMenu2[ii]);
            }

            contextMenuStrip1.Items.Add(mainMenu);
            contextMenuStrip1.Items.Add(mainMenu2);

            ContextMenuStrip = contextMenuStrip1;

            //OnseiNinshiki();
        }

        public void MainLoop()
        {
            DX.ClearDrawScreen(); //裏画面を消す

            DX.DrawBox(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, DX.GetColor(1, 1, 1), DX.TRUE); //背景を設定(透過させる)

            playTime += playSpeed;

            //モーションの再生位置が終端まで来たら最初に戻す
            if (playTime >= totalTime)
            {
                playTime = 0.0f;
            }

            DX.MV1SetAttachAnimTime(modelHandle, attachIndex, playTime); //モーションの再生位置を設定

            ModelMove();

            if (cameraHandle > 0)
            {
                //VMDカメラモーションのパラメータをDXライブラリの設定に反映させる
                //SetupVMDCameraMotionParam(cameraHandle, playTime);
            }
            if (soundHandle > 0 && DX.CheckSoundMem(soundHandle) == 0)
            {
                DX.DeleteSoundMem(soundHandle);
            }

            if (modelHandle != 0)
            {
                DX.MV1DrawModel(modelHandle); //3Dモデルの描画
            }
            
            //ESCキーを押したら終了
            if (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) != 0)
            {
                Close();
            }

            DX.ScreenFlip(); //裏画面を表画面にコピー
        }

        /// <summary>
        /// デスクトップマスコット終了
        /// </summary>
        /// <param name="sender">イベントを発生させたオブジェクトへの参照</param>
        /// <param name="e">イベントハンドラー</param>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            DX.DxLib_End();//DxLibの終了処理
        }

        /// <summary>
        /// フォーム読み込み時の処理
        /// </summary>
        /// <param name="sender">イベントを発生させたオブジェクトへの参照</param>
        /// <param name="e">イベントハンドラー</param>
        private void Form1_Load(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.None;  //フォームの枠を非表示にする
            Bitmap bmp = new Bitmap(@"form.bmp");    //画像を読み込み
            Color transColor = bmp.GetPixel(0, 0);
            bmp.MakeTransparent(transColor);         //画像を透明にする
            BackgroundImage = bmp;                   //背景画像を指定する
            BackColor = transColor;                  //背景色を指定する
            TransparencyKey = transColor;            //透明を指定する
        }

        /*private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされた場合のみ受け付け
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] path = (string[])e.Data.GetData(DataFormats.FileDrop, false); //ドロップされたファイルのパスを取得する
            var ids = new List<long>();

            //各画像をアップロードしIDを取得
            foreach(var p in path)
            {
                MediaUploadResult image = token.Media.Upload(media: new FileInfo(p));
                ids.Add(image.MediaId);
            }

            //画像をツイートする
            Status s = token.Statuses.Update(status: "Upload Image", media_ids: ids);
        }*/

        /// <summary>
        /// マウスクリック時のモデルの移動処理
        /// </summary>
        private void ModelMove()
        {
            int MouseX, MouseY;

            DX.GetMousePoint(out MouseX, out MouseY);

            PrevInput = NowInput;
            NowInput = DX.GetMouseInput();
            EdgeInput = NowInput & ~PrevInput;

            //既にモデルを掴んでいるかどうかで処理を分岐
            if (Catch == 0)
            {
                // 掴んでいない場合
                //左クリックされたらモデルをクリックしたかを調べる
                if ((EdgeInput & DX.MOUSE_INPUT_1) != 0)
                {
                    DX.VECTOR ScreenPos1;
                    DX.VECTOR ScreenPos2;
                    DX.VECTOR WorldPos1;
                    DX.VECTOR WorldPos2;

                    //モデルとの当たり判定用の線分の２座標を作成
                    ScreenPos1.x = (float)MouseX;
                    ScreenPos1.y = (float)MouseY;
                    ScreenPos1.z = 0.0f;

                    ScreenPos2.x = (float)MouseX;
                    ScreenPos2.y = (float)MouseY;
                    ScreenPos2.z = 1.0f;

                    WorldPos1 = DX.ConvScreenPosToWorldPos(ScreenPos1);
                    WorldPos2 = DX.ConvScreenPosToWorldPos(ScreenPos2);

                    //モデルの当たり判定情報を更新
                    DX.MV1RefreshCollInfo(modelHandle, -1);
                    DX.MV1_COLL_RESULT_POLY Result = DX.MV1CollCheck_Line(modelHandle, -1, WorldPos1, WorldPos2);

                    if (Result.HitFlag == DX.TRUE)
                    {
                        Catch = 1;

                        //掴んだときのスクリーン座標を保存
                        CatchMouseX = MouseX;
                        CatchMouseY = MouseY;

                        //掴んだときのモデルのワールド座標を保存
                        Catch3DModelPosition = DX.MV1GetPosition(modelHandle);

                        //掴んだときのモデルと線分が当たった座標を保存( 座標をスクリーン座標に変換したものも保存しておく )
                        Catch3DHitPosition = Result.HitPosition;
                        Catch2DHitPosition = DX.ConvWorldPosToScreenPos(Catch3DHitPosition);
                    }
                }
            }
            else
            {
                //掴んでいる場合
                //マウスの左クリックが離されていたら掴み状態を解除
                if ((NowInput & DX.MOUSE_INPUT_1) == 0)
                {
                    Catch = 0;
                }
                else
                {
                    //掴み状態が継続していたらマウスカーソルの移動に合わせてモデルも移動
                    float MoveX, MoveY;
                    DX.VECTOR NowCatch2DHitPosition;
                    DX.VECTOR NowCatch3DHitPosition;
                    DX.VECTOR Now3DModelPosition;

                    //掴んだときのマウス座標から現在のマウス座標までの移動分を算出
                    MoveX = (float)(MouseX - CatchMouseX);
                    MoveY = (float)(MouseY - CatchMouseY);

                    //掴んだときのモデルと線分が当たった座標をスクリーン座標に変換したものにマウスの移動分を足す
                    NowCatch2DHitPosition.x = Catch2DHitPosition.x + MoveX;
                    NowCatch2DHitPosition.y = Catch2DHitPosition.y + MoveY;
                    NowCatch2DHitPosition.z = Catch2DHitPosition.z;

                    //掴んだときのモデルと線分が当たった座標をスクリーン座標に変換したものにマウスの移動分を足した座標をワールド座標に変換
                    NowCatch3DHitPosition = DX.ConvScreenPosToWorldPos(NowCatch2DHitPosition);

                    //掴んだときのモデルのワールド座標に『掴んだときのモデルと線分が当たった座標にマウスの移動分を足した座標をワールド座標に
                    //変換した座標』と、『掴んだときのモデルと線分が当たった座標』との差分を加算
                    Now3DModelPosition.x = Catch3DModelPosition.x + NowCatch3DHitPosition.x - Catch3DHitPosition.x;
                    Now3DModelPosition.y = Catch3DModelPosition.y + NowCatch3DHitPosition.y - Catch3DHitPosition.y;
                    Now3DModelPosition.z = Catch3DModelPosition.z + NowCatch3DHitPosition.z - Catch3DHitPosition.z;

                    //↑の計算で求まった新しい座標をモデルの座標としてセット
                    DX.MV1SetPosition(modelHandle, Now3DModelPosition);
                }
            }
        }

        /// <summary>
        /// マウスホイールによるモデルの拡大縮小処理
        /// </summary>
        /// <param name="sender">イベントを発生させたオブジェクトへの参照</param>
        /// <param name="e">イベントハンドラー</param>
        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            float wheel = 0;
            wheel = e.Delta / 120;
            rot = 0.01f;
            //回転量で拡大か縮小か判断する
            if (wheel > 0)
            {
                scale.x += rot;
                scale.y += rot;
                scale.z += rot;
                DX.MV1SetScale(modelHandle, scale);
            }
            else if(wheel < 0)
            {
                scale.x -= rot;
                scale.y -= rot;
                scale.z -= rot;
                DX.MV1SetScale(modelHandle, scale);
            }
        }

        private void ContextMenu_Click(object sender, ToolStripItemClickedEventArgs e)
        {
            // 第1階層のクリックイベントのみで発生する
            // 第2階層のクリックではこのイベントは発生しない
        }

        /// <summary>
        /// メニューからのモデルの切り替えの処理
        /// </summary>
        /// <param name="sender">イベントを発生させたオブジェクトへの参照</param>
        /// <param name="e">イベントハンドラー</param>
        private void contextMenuStrip_SubMenuClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            //メニューのインデックスを取得
            int index = ((ToolStripDropDownMenu)item.Owner).Items.IndexOf(item);

            //選択されたモデルが今のモデルと違う場合のみ変更
            if(nowModelIndex != index)
            {
                DX.MV1DeleteModel(modelHandle);
                nowModelIndex = index;
                System.Random rand = new System.Random();
                motionNum = rand.Next(maxNum);
                modelHandle = DX.MV1LoadModel(modelMenu[index].modelPass);
                attachIndex = DX.MV1AttachAnim(modelHandle, motionNum, -1, DX.FALSE);
                totalTime = DX.MV1GetAttachAnimTotalTime(modelHandle, attachIndex);
            }
            else
            {
                string message = "同じモデルです";
                MessageBox.Show(this, message, "メニュー", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// メニューから音楽再生の処理
        /// </summary>
        /// <param name="sender">イベントを発生させたオブジェクトへの参照</param>
        /// <param name="e">イベントハンドラー</param>
        private void contextMenuStrip_SubMenu2Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            int index = ((ToolStripDropDownMenu)item.Owner).Items.IndexOf(item);
            musicIndex = index;
            if(soundHandle > 0)
            {
                DX.DeleteSoundMem(soundHandle);
            }
            switch(musicIndex)
            {
                case (int)MusicNum.KOIKAZE:
                    DX.MV1DetachAnim(modelHandle, attachIndex);
                    soundHandle = DX.LoadSoundMem("Data/Music/koikaze.mp3");
                    DX.PlaySoundMem(soundHandle, DX.DX_PLAYTYPE_BACK);
                    cameraHandle = DX.MV1LoadModel("Data/Koikaze_Camera.vmd");
                    motionNum = (int)MotionNum.KOIKAZE;
                    attachIndex = DX.MV1AttachAnim(modelHandle, motionNum, -1, DX.FALSE);
                    totalTime = DX.MV1GetAttachAnimTotalTime(modelHandle, attachIndex);
                    playTime = 0.0f;
                    DX.MV1SetRotationXYZ(modelHandle, DX.VGet(0.0f, 0.0f, 0.0f));
                    break;
            }

        }

        /// <summary>
        /// 設定ファイルの読み取り
        /// </summary>
        /// <param name="filePass">読み取りたいファイルのパス</param>
        /// <returns>読み取ったパラメータ</returns>
        private List<string> LoadSettingFile(string filePass)
        {
            StreamReader fileHandle = new StreamReader(filePass, Encoding.UTF8);
            var list = new List<string>();
            if(fileHandle != null)
            {
                while (!fileHandle.EndOfStream)
                {
                    string line = fileHandle.ReadLine();
                    list.Add(line);
                }
                fileHandle.Close();
            }
            return list;
        }

        
        private void SetupVMDCameraMotionParam(int cameraHandle, float time)
        {
            DX.MATRIX VrotMat, HrotMat, MixrotMat, TwistrotMat;
            DX.VECTOR camLoc, camDir, camUp;
            DX.VECTOR location, rotation;
            float length, viewAngle;

            //カメラの注視点を取得
            location = DX.MV1GetAnimKeyDataToVectorFromTime(cameraHandle, 0, time);

            //カメラの回転値を取得
            rotation = DX.MV1GetAnimKeyDataToVectorFromTime(cameraHandle, 1, time);

            //カメラの注視点までの距離の符号を逆転したものを取得
            length = DX.MV1GetAnimKeyDataToLinearFromTime(cameraHandle, 2, time);

            //カメラの視野角を取得
            viewAngle = DX.MV1GetAnimKeyDataToLinearFromTime(cameraHandle, 3, time);

            //垂直方向の回転行列を取得
            VrotMat = DX.MGetRotX(-rotation.x);

            //水平方向の回転行列を取得
            HrotMat = DX.MGetRotY(-rotation.y);

            //垂直方向の回転行列と水平方向の回転行列を合成
            MixrotMat = DX.MMult(VrotMat, HrotMat);

            //カメラの向きを算出
            camDir = DX.VTransform(DX.VGet(0.0f, 0.0f, 1.0f), MixrotMat);

            //捻り回転行列を取得
            TwistrotMat = DX.MGetRotAxis(camDir, -rotation.z);

            //捻り回転行列を合成行列に合成
            MixrotMat = DX.MMult(MixrotMat, TwistrotMat);

            //カメラの上向きを算出
            camUp = DX.VTransform(DX.VGet(0.0f, 1.0f, 0.0f), MixrotMat);

            //カメラの座標を算出
            camLoc = DX.VTransform(DX.VGet(0.0f, 0.0f, length), MixrotMat);
            camLoc = DX.VAdd(camLoc, location);

            //注視点を算出
            location = DX.VAdd(camLoc, camDir);

            //視野角をセット
            DX.SetupCamera_Perspective(viewAngle / 180.0f * (float)Math.PI);

            //カメラの座標と注視点と上方向をセット
            DX.SetCameraPositionAndTargetAndUpVec(camLoc, location, camUp);
        }
    }
}
