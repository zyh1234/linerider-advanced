﻿//
//  LineAdjustTool.cs
//
//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Gwen.Controls;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using linerider.Lines;
using linerider.UI;
using linerider.Utils;
namespace linerider.Tools
{
    public class MoveTool : Tool
    {
        struct SelectInfo
        {
            public Vector2d start;
            public GameLine line;
            //     public Line snap;
            public bool leftjoint;
            public bool rightjoint;

        }
        public bool CanLifelock => UI.InputUtils.Check(Hotkey.ToolLifeLock);
        private SelectInfo _selection;
        private bool _started = false;
        private GameLine _before;
        //   private LineState _before_snap;
        public override MouseCursor Cursor
        {
            get { return game.Cursors["adjustline"]; }
        }

        public bool Started
        {
            get
            {
                return _started;
            }
        }


        public MoveTool()
        {
        }


        public void Deselect()
        {
        }

        public void MoveSelection(Vector2d pos)
        {
            if (_selection.line != null)
            {
                var line = _selection.line;
                using (var trk = game.Track.CreateTrackWriter())
                {
                    trk.DisableUndo();
                    var left = _selection.leftjoint ? _before.Position + (pos - _selection.start) : line.Position;
                    var right = _selection.rightjoint ? _before.Position2 + (pos - _selection.start) : line.Position2;
                    if (_selection.leftjoint != _selection.rightjoint)
                    {
                        var start = _selection.leftjoint ? right : left;
                        var end = _selection.rightjoint ? right : left;
                        var currentdelta = _selection.line.Position2 - _selection.line.Position;
                        if (UI.InputUtils.Check(Hotkey.ToolAngleLock))
                        {
                            end = Utility.AngleLock(start, end, Angle.FromVector(currentdelta));
                        }
                        if (UI.InputUtils.Check(Hotkey.ToolXYSnap))
                        {
                            end = Utility.SnapToDegrees(start, end);
                        }
                        if (UI.InputUtils.Check(Hotkey.ToolLengthLock))
                        {
                            end = Utility.LengthLock(start, end, currentdelta.Length);
                        }
                        if (_selection.rightjoint)
                            right = end;
                        else
                            left = end;
                    }
                    trk.MoveLine(line,
                    left,
                    right);
                }
                if (line is StandardLine && CanLifelock)
                {
                    game.Track.BufferManager.UpdateOnThisThread();
                    using (var trk = game.Track.CreateTrackWriter())
                    {
                        if (LifeLock(trk, (StandardLine)line))
                        {
                            Stop();
                        }
                    }
                }
                else
                {
                    game.Track.NotifyTrackChanged();
                }
            }
            game.Invalidate();
        }

        public override void OnMouseDown(Vector2d mousepos)
        {
            Stop();//double check
            var gamepos = MouseCoordsToGame(mousepos);
            using (var trk = game.Track.CreateTrackWriter())
            {
                _selection = new SelectInfo();
                var line = SelectLine(trk, gamepos);
                if (line != null)
                {
                    _before = line.Clone();
                    var point = Utility.CloserPoint(gamepos, line.Position, line.Position2);//TrySnapPoint(trk, gamepos);
                    //is it a knob?
                    if ((gamepos - point).Length <= line.Width)
                    {
                        _selection.start = gamepos;
                        _selection.line = line;
                        if (InputUtils.Check(Hotkey.ToolSelectBothJoints))
                        {
                            _selection.leftjoint = _selection.rightjoint = true;
                        }
                        else
                        {
                            _selection.leftjoint = line.Position == point;
                            if (!_selection.leftjoint)
                            {
                                _selection.rightjoint = line.Position2 == point;
                            }
                        }
                    }
                    else
                    {
                        //select whole line
                    }
                }
                if (_selection.leftjoint || _selection.rightjoint)
                    _started = true;
            }
            base.OnMouseDown(gamepos);
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (_started)
            {
                MoveSelection(MouseCoordsToGame(pos));
            }
            base.OnMouseMoved(pos);
        }

        public override void OnMouseRightDown(Vector2d pos)
        {
            base.OnMouseRightDown(pos);
        }

        public override void OnMouseUp(Vector2d pos)
        {
            Stop();
            base.OnMouseUp(pos);
        }

        public override void Stop()
        {
            _started = false;
            if (_selection.line != null)
            {
                game.Track.UndoManager.BeginAction();
                game.Track.UndoManager.AddChange(_before, _selection.line);
                game.Track.UndoManager.EndAction();
            }
            _selection.line = null;
        }
    }
}