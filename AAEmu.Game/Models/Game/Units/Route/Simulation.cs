﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char.Templates;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units.Movements;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.Tasks.UnitMove;
using AAEmu.Game.Utils;

using NLog;

namespace AAEmu.Game.Models.Game.Units.Route
{
    /// <summary>
    /// 模拟线路
    /// Analog circuit
    /// 这里的模拟线路指由开发人员进行手动收集所行走的路线然后保存。
    /// Analog lines here refer to routes that are collected manually by developers and then saved.
    /// 控制NPC按照这种路线进行移动
    /// Control NPC to move along this route
    /// </summary>
    public class Simulation : Patrol
    {
        public Simulation(Npc npc)
        {
            Init(npc);
        }

        private static Logger _log = LogManager.GetCurrentClassLogger();

        public float myX, myY, myZ; // наши статы

        public LineTo Line { get; set; }

        //// movement data
        public List<string> MovePath;     //  данные по которому мы будем двигатся в данный момент
        public List<string> RecordPath;   //  данные для записи пути
        public string RecordPathFileName = @"recordfile"; // название файла для записи
        public string MovePathFileName = @"movefile";   // название файла для записи
        public static int PointsCount;           // кол-во поинтов в процессе записи пути

        public bool SavePathEnabled;      // флаг записи пути

        public bool MoveToPathEnabled;    // флаг движения по пути
        public bool MoveToForward;        // направление движения да-вперед, нет - назад
        public int MoveStepIndex;         // текущ. чекпоинт (куды бежим сейчас)
        //MoveTimer: TTimer;              //

        int oldTime, chkTime;
        float oldX, oldY, oldZ;
        //*******************************************************
        string RecordFilesPath = @"d:\";       // путь где хранятся наши файлы
        string RecordPathName = @"recordfile"; // файл по умолчанию
        string RecordFileExt = @".txt";        // расширение по умолчанию
        string MoveFilesPath = @"d:\";       // путь где хранятся наши файлы
        string MovePathName = @"movefile";   // файл по умолчанию
        string MoveFileExt = @".txt";        // расширение по умолчанию

        int RangeToCheckPoint = 150; // дистанция до чекпоинта при которой считается , что мы достигли оного
        int MoveTrigerDelay = 800;   // срабатывание таймера на движение  0,8 сек

        string cmdPrefix = "===";    // с этих символов начинается команда
        string cmdDlm = " ";         // разделитель параметров команды, параметры не должены содержать разделитель
        // команды в общий чат
        // пример
        string cmdRecordPath = "rec";   // в общий чат "===rec giran1"
        string cmdSavePath = "save";    // в общий чат "===save"
        string cmdMove = "go";          // в общий чат "===go giran1"
        string cmdBack = "back";        // в общий чат "===back giran1"
        string cmdStop = "stop";        // останавливает следование по маршруту
        //*******************************************************

        /*
           by alexsl
           маленькая лепта в скриптоводстве, возможно кому-нибудь понадобится.
           
           что делает:
           - автоматически записывает в файл маршрут следования;
           - можно загрузить данные пути из файла;
           - передвигается по маршруту.
           
           Для начала, нужно создать маршрут(ы), запись происходит так:
           1. Начать запись;
           2. Пройтись по маршруту;
           3. Остановить запись.
           === вот примерная структкра файла (x,y,z)=========
           |19007|145255|-3151|
           |19016|144485|-3130|
           |19124|144579|-3128|
           |19112|144059|-3103|
           ==================================================
           */

        float distance = 1.5f;
        float MovingDistance = 0.27f;

        public override void Execute(Npc npc)
        {
            OnMove(npc);
        }
        //***************************************************************
        //ПЕРЕМЕЩЕНИЕ:
        //Идти в точку с координатами x,y,z
        //MOVETO(npc, x, y, z)
        public void MoveTo(Npc npc, float TargetX, float TargetY, float TargetZ)
        {
            var Seq = (uint)Rand.Next(0, 10000);
            var moveType = (UnitMoveType)MoveType.GetType(MoveTypeEnum.Unit);

            moveType.X = TargetX;
            moveType.Y = TargetY;
            moveType.Z = TargetZ;
            //--------------------взгляд_персонажа_будет(движение_куда<-движение_откуда)
            var angle = MathUtil.CalculateAngleFrom(TargetX, TargetY, npc.Position.X, npc.Position.Y);
            var rotZ = MathUtil.ConvertDegreeToDirection(angle);
            moveType.RotationX = 0;
            moveType.RotationY = 0;
            moveType.RotationZ = rotZ;

            moveType.Flags = 5;
            moveType.DeltaMovement = new sbyte[3];
            moveType.DeltaMovement[0] = 0;
            moveType.DeltaMovement[1] = 127;
            moveType.DeltaMovement[2] = 0;
            moveType.Stance = 1;    //combat=0, idle=1
            moveType.Alertness = 0; //idle=0, combat=2
            moveType.Time = Seq;

            npc.BroadcastPacket(new SCOneUnitMovementPacket(npc.ObjId, moveType), true);

            //npc.SendPacket(new SCOneUnitMovementPacket(npc.ObjId, moveType));

            //npc.DisabledSetPosition = true;
            //npc.SendPacket(new SCTeleportUnitPacket(0, 0, TargetX, TargetY, TargetZ, 0f));

        }

        //***************************************************************
        //возвращает растоянием между 2 точками
        public int Delta(float positionX1, float positionY1, float positionX2, float positionY2)
        {
            //return Math.Round(Math.Sqrt((positionX1-positionX2)*(positionX1-positionX2))+(positionY1-positionY2)*(positionY1-positionY2));
            var dx = positionX1 - positionX2;
            var dy = positionY1 - positionY2;
            var summa = dx * dx + dy * dy;
            if (Math.Abs(summa) < tolerance)
            {
                return 0;
            }

            return (int)Math.Round(Math.Sqrt(summa));
        }
        //***************************************************************
        //Ориентация на местности: Проверка находится ли заданная точка в пределах досягаемости
        //public bool PosInRange(Npc npc, float targetX, float targetY, float targetZ, int distance)
        //***************************************************************
        public bool PosInRange(Npc npc, float targetX, float targetY, int distance)
        {
            return Delta(targetX, targetY, npc.Position.X, npc.Position.Y) <= distance;
        }
        //***************************************************************
        public string GetValue(string valName)
        {
            return RecordPath.Find(x => x == valName);
        }
        //***************************************************************
        public void SetValue(string valName, string value)
        {
            var index = RecordPath.IndexOf(RecordPath.Where(x => x == valName).FirstOrDefault());
            RecordPath[index] = value;
        }
        //***************************************************************
        public float ExtractValue(string sData, int nIndex)
        {
            int i;
            var j = 0;
            var s = sData;
            while (j < nIndex)
            {
                i = s.IndexOf('|');
                if (i >= 0)
                {
                    s = s.Substring(i + 1, s.Length - (i + 1));
                    j++;
                }
                else
                {
                    break;
                }
            }
            i = s.IndexOf('|');
            if (i >= 0)
            {
                s = s.Substring(0, i - 1);
            }
            var result = Convert.ToSingle(s);
            return result;
        }
        //***************************************************************
        public int GetMinCheckPoint(Npc npc, List<string> pointsList)
        {
            int m, minDist;
            string s;

            var result = -1;
            minDist = -1;
            // проверка на наличие маршрута
            if (pointsList.Count == 0)
            {
                _log.Warn("Нет данных по маршруту");
                return -1;
            }

            for (var i = 0; i < pointsList.Count - 1; i++)
            {
                s = pointsList[i];
                Line.Position.Y = ExtractValue(s, 2);
                Line.Position.X = ExtractValue(s, 1);

                _log.Warn(s + " x:=" + Line.Position.X + " y:=" + Line.Position.Y);

                m = Delta(Line.Position.X, Line.Position.Y, npc.Position.X, npc.Position.Y);

                if (m <= 0) { continue; }

                if (result == -1)
                {
                    minDist = m;
                    result = i;
                }
                else if (m < minDist)
                {
                    minDist = m;
                    result = i;
                }
            }
            return result;
        }
        //***************************************************************
        public void StartRecord()
        {
            if (SavePathEnabled)
            {
                return;
            }

            if (MoveToPathEnabled)
            {
                _log.Warn("Во время следования по маршруту запись не возможна");
                return;
            }
            RecordPath.Clear();
            PointsCount = 0;
            _log.Warn("Начата запись маршрута");
            SavePathEnabled = true;
        }
        //***************************************************************
        public void StopRecord()
        {
            if (!SavePathEnabled)
            {
                return;
            }
            // записываем в файл
            using (StreamWriter sw = new StreamWriter(GetRecordFileName()))
            {
                foreach (var b in RecordPath)
                {
                    sw.WriteLine(b.ToString());
                }
            }
            _log.Warn("Запись маршрута завершена");
            SavePathEnabled = false;
        }
        //***************************************************************
        public string GetRecordFileName()
        {
            var result = RecordFilesPath + RecordPathFileName + RecordFileExt;
            return result;
        }
        public string GetMoveFileName()
        {
            var result = MoveFilesPath + MovePathFileName + MoveFileExt;
            return result;
        }
        //***************************************************************
        public void ParseMoveClient(Npc npc)
        {
            if (!SavePathEnabled) { return; }
            Line.Position.X = npc.Position.X;
            Line.Position.Y = npc.Position.Y;
            Line.Position.Z = npc.Position.Z;
            var s = "|" + Line.Position.X + "|" + Line.Position.Y + "|" + Line.Position.Z + "|";
            RecordPath.Add(s);
            PointsCount++;
            _log.Warn("добавлен чекпоинт № {0}", PointsCount);
        }
        //***************************************************************
        public void GoToPath(Npc npc, bool ToForward)
        {
            MoveToPathEnabled = !MoveToPathEnabled;
            MoveToForward = ToForward;
            if (!MoveToPathEnabled)
            {
                //MoveTimer.Enabled:=False;
                _log.Warn("Следование по маршруту остановлено");
                Line.Stop(npc);
                return;
            }
            //предположительно путь уже прописан в MovePath
            _log.Warn("Пробуем выйти на путь...");
            //сперва идем к ближайшему чекпоинту
            var i = GetMinCheckPoint(npc, MovePath);
            if (i < 0)
            {
                _log.Warn("чекпоинт не найден");
                MoveToPathEnabled = false;
                Line.Stop(npc);
                return;
            }

            _log.Warn("найден ближайший чекпоинт #" + i + " бежим туда");
            MoveToPathEnabled = true;
            MoveStepIndex = i;
            _log.Warn("checkpoint #" + i);
            var s = MovePath[MoveStepIndex];
            Line.Position.X = ExtractValue(s, 1);
            Line.Position.Y = ExtractValue(s, 2);
            Line.Position.Z = ExtractValue(s, 3);
            if (Math.Abs(oldX - Line.Position.X) > tolerance && Math.Abs(oldY - Line.Position.Y) > tolerance && Math.Abs(oldZ - Line.Position.Z) > tolerance)
            {
                //MoveTo(npc, Line.Position.X, Line.Position.Y, Line.Position.Z);
                oldX = Line.Position.X;
                oldY = Line.Position.Y;
                oldZ = Line.Position.Z;
                oldTime = 0;
            }
            //MoveTimer.Enabled:=True; // TODO
            MoveTo(npc, Line.Position.X, Line.Position.Y, Line.Position.Z);
            Repeat(npc, 1000, Line);
            chkTime = 0;
        }
        //***************************************************************
        public void OnMove(Npc npc)
        {
            if (!MoveToPathEnabled)
            {
                //TTimer(Sender).Enabled:=False;
                _log.Warn("OnMove disabled");
                Line.Stop(npc);
                return;
            }
            try
            {
                MovePath.Count();// проверяем на существование объекта, при отладке всякое может быть
            }
            catch (Exception e)
            {
                //TTimer(Sender).Enabled:=False;
                _log.Warn("Error: {0}", e);
                Line.Stop(npc);
                return;
            }
            var s = MovePath[MoveStepIndex];
            Line.Position.X = ExtractValue(s, 1);
            Line.Position.Y = ExtractValue(s, 2);
            Line.Position.Z = ExtractValue(s, 3);
            if (!PosInRange(npc, Line.Position.X, Line.Position.Y, 1))
            {
                MoveTo(npc, Line.Position.X, Line.Position.Y, Line.Position.Z);
                Repeat(npc, 1000, Line);
                return;
            }
            if (MoveToForward)
            {
                if (MoveStepIndex == MovePath.Count - 1)
                {
                    MoveToPathEnabled = false;
                    _log.Warn("Мы по идее в конечной точке");
                    Line.Stop(npc);
                    return;
                }
                MoveStepIndex++;
            }
            else
            {
                if (MoveStepIndex > 0)
                {
                    MoveStepIndex--;
                }
                else
                {
                    MoveToPathEnabled = false;
                    _log.Warn("Мы по идее в начальной точке");
                    Line.Stop(npc);
                    return;
                }
                //end;
                _log.Warn("мы достигли чекпоинта идем далее");
                _log.Warn("бежим к #" + MoveStepIndex);
                s = MovePath[MoveStepIndex];
                Line.Position.X = ExtractValue(s, 1);
                Line.Position.Y = ExtractValue(s, 2);
                Line.Position.Z = ExtractValue(s, 3);
            }
            MoveTo(npc, Line.Position.X, Line.Position.Y, Line.Position.Z);
            Repeat(npc, 1000, Line);
        }

        public void Init(Npc npc) //Вызывается при включении скрипта
        {
            Line = new LineTo
            {
                Interrupt = true,
                Loop = false,
                LastPatrol = Line,
                Position = npc.Position.Clone()
            };
            npc.Patrol = Line;

            try
            {
                MovePath = new List<string>();
                MovePath = File.ReadLines(GetMoveFileName()).ToList();
            }
            catch (Exception e)
            {
                _log.Warn("Error in read MovePath: {0}", e);
                Line.Stop(npc);
            }

            try
            {
                RecordPath = new List<string>();
                RecordPath = File.ReadLines(GetMoveFileName()).ToList();
            }
            catch (Exception e)
            {
                _log.Warn("Error in read RecordPath: {0}", e);
                Line.Stop(npc);
            }
        }
    }
}
