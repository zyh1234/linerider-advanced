﻿//
//  GameRenderer.cs
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

using linerider.Tools;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using linerider.Game;
using linerider.Utils;
using linerider.Drawing;
using linerider.Lines;

namespace linerider.Rendering
{
    public static class GameRenderer
    {
        #region Fields

        public static MainWindow Game;
        private static readonly VAO _roundlinevao = new VAO(false, true);

        #endregion Fields

        #region Methods
        public static void DrawRider(float opacity, Rider rider, bool scarf = false, bool drawcontactpoints = false, bool momentumvectors = false, int iteration = 6)
        {
            if (scarf)
            {
                DrawScarf(rider.GetScarfLines(), opacity);
            }
            var points = rider.Body;

            DrawTexture(Models.LegTexture, Models.LegRect,
            points[RiderConstants.BodyButt].Location,
            points[RiderConstants.BodyFootRight].Location, opacity);

            DrawTexture(Models.ArmTexture, Models.ArmRect,
            points[RiderConstants.BodyShoulder].Location,
            points[RiderConstants.BodyHandRight].Location, opacity);
            if (!rider.Crashed)
                RenderRoundedLine(points[RiderConstants.BodyHandRight].Location, points[RiderConstants.SledTR].Location,
                Color.Black, 0.1f);

            if (rider.SledBroken)
            {
                DrawTexture(Models.BrokenSledTexture, Models.SledRect,
                points[RiderConstants.SledTL].Location,
                points[RiderConstants.SledTR].Location, opacity);
            }
            else
            {
                DrawTexture(Models.SledTexture, Models.SledRect,
                points[RiderConstants.SledTL].Location,
                points[RiderConstants.SledTR].Location, opacity);
            }

            DrawTexture(Models.LegTexture, Models.LegRect,
            points[RiderConstants.BodyButt].Location,
            points[RiderConstants.BodyFootLeft].Location, opacity);
            if (!rider.Crashed)
            {
                DrawTexture(Models.BodyTexture, Models.BodyRect,
                points[RiderConstants.BodyButt].Location,
                points[RiderConstants.BodyShoulder].Location, opacity);
            }
            else
            {
                DrawTexture(Models.BodyDeadTexture, Models.BodyRect,
                points[RiderConstants.BodyButt].Location,
                points[RiderConstants.BodyShoulder].Location, opacity);
            }
            if (!rider.Crashed)
                RenderRoundedLine(points[RiderConstants.BodyHandLeft].Location, points[RiderConstants.SledTR].Location,
                Color.Black, 0.1f);

            DrawTexture(Models.ArmTexture, Models.ArmRect,
            points[RiderConstants.BodyShoulder].Location,
            points[RiderConstants.BodyHandLeft].Location, opacity);
        }
        public static void DrawMomentum(Rider rider, List<GenericVertex> vertices)
        {
            for(int i = 0; i < rider.Body.Length; i++)
            {
                var anchor = rider.Body[i];
                var vec1 = anchor.Location;
                var vec2 = vec1 + (anchor.Momentum);
                vertices.AddRange(GenRoundedLine(vec1, vec2, Color.Red, 1f / 2, false));
            }
        }
        public static void DrawContactPoints(Rider rider, List<int> diagnosis, List<GenericVertex> vertices)
        {
            if (diagnosis == null)
                diagnosis = new List<int>();
            for (var i = 0; i < RiderConstants.Bones.Length; i++)
            {
                var c = Color.FromArgb(unchecked((int)0xFFCC72B7));
                if (RiderConstants.Bones[i].Breakable)
                {
                    continue;
                }
                else if (RiderConstants.Bones[i].OnlyRepel)
                {
                    c = Color.CornflowerBlue;
                    vertices.AddRange(GenRoundedLine(rider.Body[RiderConstants.Bones[i].joint1].Location, rider.Body[RiderConstants.Bones[i].joint2].Location, c, 1f / 4, false));
                }
                else if (i <= 3)
                {
                    vertices.AddRange(GenRoundedLine(rider.Body[RiderConstants.Bones[i].joint1].Location, rider.Body[RiderConstants.Bones[i].joint2].Location, c, 1f / 4, false));
                }
            }
            if (!rider.Crashed && diagnosis.Count != 0)
            {
                Color firstbreakcolor = Color.FromArgb(unchecked((int)0xFFFF8C00));
                Color breakcolor = Color.FromArgb(unchecked((int)0xff909090)); ;

                for (int i = 1; i < diagnosis.Count; i++)
                {
                    var broken = diagnosis[i];
                    vertices.AddRange(GenRoundedLine(
                    rider.Body[RiderConstants.Bones[broken].joint1].Location,
                    rider.Body[RiderConstants.Bones[broken].joint2].Location, breakcolor, 1f / 4, false));
                }
                //the first break is most important so we give it a better color, assuming its not just a fakie death
                if (diagnosis[0] > 0)
                {
                    vertices.AddRange(GenRoundedLine(
                    rider.Body[RiderConstants.Bones[diagnosis[0]].joint1].Location,
                    rider.Body[RiderConstants.Bones[diagnosis[0]].joint2].Location,
                    firstbreakcolor, 1f / 4, false));
                }
            }
            for (var i = 0; i < RiderConstants.Bones.Length; i++)
            {
                Color c = Color.Cyan;
                if (
                    ((i == RiderConstants.SledTL || i == RiderConstants.SledBL) && diagnosis.Contains(-1)) ||
                    ((i == RiderConstants.BodyButt || i == RiderConstants.BodyShoulder) && diagnosis.Contains(-2)))
                {
                    c = Color.Blue;
                }
                vertices.AddRange(GenRoundedLine(rider.Body[RiderConstants.Bones[i].joint1].Location, rider.Body[RiderConstants.Bones[i].joint1].Location, c, 1f / 4, false));
            }
        }
        public static void DrawScarf(Line[] lines, float opacity)
        {
            GLEnableCap blend = null;
            if (opacity < 1)
            {
                blend = new GLEnableCap(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            }
            GameDrawingMatrix.Enter();
            VAO scarf = new VAO(false, true);//VAO does not need disposing, it does not allocate a buffer
            List<Vector2> altvectors = new List<Vector2>();
            Color c = Color.FromArgb((byte)(255 * opacity), 209, 1, 1);
            var alt = Color.FromArgb((byte)(255 * opacity), 255, 100, 100);
            for (int i = 0; i < lines.Length; i += 2)
            {
                var thickline = StaticRenderer.GenerateThickLine((Vector2)lines[i].Position, (Vector2)lines[i].Position2, 2);

                GenericVertex tl = (new GenericVertex(thickline[0], c));
                GenericVertex tr = (new GenericVertex(thickline[1], c));
                GenericVertex br = (new GenericVertex(thickline[2], c));
                GenericVertex bl = (new GenericVertex(thickline[3], c));

                scarf.AddVertex(tl);
                scarf.AddVertex(bl);
                scarf.AddVertex(tr);

                scarf.AddVertex(bl);
                scarf.AddVertex(tr);
                scarf.AddVertex(br);
                if (i != 0)
                {
                    altvectors.Add(tl.Position);
                    altvectors.Add(bl.Position);
                }
                altvectors.Add(br.Position);
                altvectors.Add(tr.Position); ;
            }
            for (int i = 0; i < altvectors.Count - 4; i += 4)
            {
                scarf.AddVertex(new GenericVertex(altvectors[i], alt));
                scarf.AddVertex(new GenericVertex(altvectors[i + 1], alt));
                scarf.AddVertex(new GenericVertex(altvectors[i + 2], alt));


                scarf.AddVertex(new GenericVertex(altvectors[i], alt));
                scarf.AddVertex(new GenericVertex(altvectors[i + 2], alt));
                scarf.AddVertex(new GenericVertex(altvectors[i + 3], alt));
            }
            scarf.Draw(PrimitiveType.Triangles);
            GameDrawingMatrix.Exit();

            if (blend != null)
                blend.Dispose();
        }

        public static void DrawTrackLine(StandardLine line, Color color, bool drawwell, bool drawcolor, bool drawknobs, bool redknobs = false)
        {
            color = Color.FromArgb(255, color);
            var thickness = 2;
            Color color2;
            var type = line.Type;
            switch (type)
            {
                case LineType.Blue:
                    color2 = Color.FromArgb(0, 0x66, 0xFF);
                    break;

                case LineType.Red:
                    color2 = Color.FromArgb(0xCC, 0, 0);
                    break;

                default:
                    throw new Exception("Rendering Invalid Line");
            }
            if (drawcolor)
            {
                var loc3 = line.DiffNormal.X > 0 ? (Math.Ceiling(line.DiffNormal.X)) : (Math.Floor(line.DiffNormal.X));
                var loc4 = line.DiffNormal.Y > 0 ? (Math.Ceiling(line.DiffNormal.Y)) : (Math.Floor(line.DiffNormal.Y));
                if (type == LineType.Red)
                {
                    var redline = line as RedLine;
                    GameDrawingMatrix.Enter();
                    GL.Color3(color2);
                    GL.Begin(PrimitiveType.Triangles);
                    var basepos = line.Position2;
                    for (int ix = 0; ix < redline.Multiplier; ix++)
                    {
                        var angle = MathHelper.RadiansToDegrees(Math.Atan2(line.Difference.Y, line.Difference.X));
                        Turtle t = new Turtle(line.Position2);
                        var basex = 8 + (ix * 2);
                        t.Move(angle, -basex);
                        GL.Vertex2(new Vector2((float)t.X, (float)t.Y));
                        t.Move(90, line.inv ? -8 : 8);
                        GL.Vertex2(new Vector2((float)t.X, (float)t.Y));
                        t.Point = line.Position2;
                        t.Move(angle, -(ix * 2));
                        GL.Vertex2(new Vector2((float)t.X, (float)t.Y));
                    }
                    GL.End();
                    GameDrawingMatrix.Exit();
                }
                RenderRoundedLine(new Vector2d(line.Position.X + loc3, line.Position.Y + loc4),
                    new Vector2d(line.Position2.X + loc3, line.Position2.Y + loc4), color2, thickness);
            }
            RenderRoundedLine(line.Position, line.Position2, color, thickness, drawknobs, redknobs);
            if (drawwell)
            {
                using (new GLEnableCap(EnableCap.Blend))
                {
                    GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                    GameDrawingMatrix.Enter();
                    GL.Begin(PrimitiveType.Quads);
                    GL.Color4(new Color4(150, 150, 150, 150));
                    var rect = StaticRenderer.GenerateThickLine((Vector2)line.Position, (Vector2)line.Position2, (float)(StandardLine.Zone * 2));

                    GL.Vertex2(line.Position);
                    GL.Vertex2(line.Position2);
                    GL.Vertex2(rect[line.inv ? 2 : 1]);
                    GL.Vertex2(rect[line.inv ? 3 : 0]);
                    GL.End();
                    GL.PopMatrix();
                }
            }
        }

        public static void RenderRoundedLine(Vector2d position, Vector2d position2, Color color, float thickness, bool knobs = false, bool redknobs = false)
        {
            using (new GLEnableCap(EnableCap.Blend))
            {
                using (new GLEnableCap(EnableCap.Texture2D))
                {
                    var vertices = GenRoundedLine(position, position2, color, thickness, knobs, redknobs);
                    if (vertices.Count != 0)
                    {
                        _roundlinevao.Texture = StaticRenderer.CircleTex;
                        _roundlinevao.Clear();
                        foreach (var v in vertices)
                            _roundlinevao.AddVertex(v);
                        _roundlinevao.SetOpacity(color.A / 255f);
                        GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
                        _roundlinevao.Draw(PrimitiveType.Triangles);
                        _roundlinevao.Texture = 0;
                    }
                }
            }
        }
        public static List<GenericVertex> GenRoundedLine(Vector2d position, Vector2d position2, Color color, float thickness, bool knobs = false, bool redknobs = false)
        {
            List<GenericVertex> vertices = new List<GenericVertex>(6 * 5);
            var end1 = (position + Game.ScreenTranslation) * Game.Track.Zoom;
            var end2 = (position2 + Game.ScreenTranslation) * Game.Track.Zoom;
            var line = StaticRenderer.GenerateThickLine((Vector2)end1, (Vector2)end2, thickness * Game.Track.Zoom);

            vertices.Add(new GenericVertex(line[0], color));
            vertices.Add(new GenericVertex(line[1], color));
            vertices.Add(new GenericVertex(line[2], color));
            vertices.Add(new GenericVertex(line[0], color));
            vertices.Add(new GenericVertex(line[3], color));
            vertices.Add(new GenericVertex(line[2], color));
            vertices.AddRange(StaticRenderer.FastCircle((Vector2)(end1), Game.Track.Zoom * (thickness / 2), color));
            vertices.AddRange(StaticRenderer.FastCircle((Vector2)(end2), Game.Track.Zoom * (thickness / 2), color));
            if (knobs)
            {
                vertices.AddRange(StaticRenderer.FastCircle((Vector2)(end1), Game.Track.Zoom * (thickness / 3), redknobs ? Color.Red : Color.White));
                vertices.AddRange(StaticRenderer.FastCircle((Vector2)(end2), Game.Track.Zoom * (thickness / 3), redknobs ? Color.Red : Color.White));
            }
            return vertices;
        }
        public static void DbgDrawGrid()
        {
            bool fastgrid = false;
            int sqsize = fastgrid ? FastGrid.CellSize : SimulationGrid.CellSize;
            GL.PushMatrix();
            GL.Scale(Game.Track.Zoom, Game.Track.Zoom, 0);
            GL.Translate(new Vector3d(Game.ScreenTranslation));
            GL.Begin(PrimitiveType.Quads);
            for (var x = -sqsize; x < (Game.RenderSize.Width / Game.Track.Zoom); x += sqsize)
            {
                for (var y = -sqsize; y < (Game.RenderSize.Height / Game.Track.Zoom); y += sqsize)
                {
                    var yv = new Vector2d(x + (Game.ScreenPosition.X - (Game.ScreenPosition.X % sqsize)), y + (Game.ScreenPosition.Y - (Game.ScreenPosition.Y % sqsize)));

                    if (!fastgrid)
                    {
                        var gridpos = new GridPoint((int)Math.Floor(yv.X / sqsize), (int)Math.Floor(yv.Y / sqsize));
                        if (Game.Track.RenderRider.PhysicsBounds.ContainsPoint(gridpos))
                        {
                            GL.Color3(Color.Lime);
                            GL.Vertex2(yv);
                            yv.Y += sqsize;
                            GL.Vertex2(yv);
                            yv.X += sqsize;
                            GL.Vertex2(yv);
                            yv.Y -= sqsize;
                            GL.Vertex2(yv);
                        }
                        else if (Game.Track.GridCheck(yv.X, yv.Y))
                        {
                            GL.Color3(Color.Yellow);
                            GL.Vertex2(yv);
                            yv.Y += sqsize;
                            GL.Vertex2(yv);
                            yv.X += sqsize;
                            GL.Vertex2(yv);
                            yv.Y -= sqsize;
                            GL.Vertex2(yv);
                        }
                    }
                    else if (Game.Track.FastGridCheck(yv.X, yv.Y))
                    {
                        GL.Color3(Color.Yellow);
                        GL.Vertex2(yv);
                        yv.Y += sqsize;
                        GL.Vertex2(yv);
                        yv.X += sqsize;
                        GL.Vertex2(yv);
                        yv.Y -= sqsize;
                        GL.Vertex2(yv);
                    }
                }
            }

            GL.End();
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(Color.Red);
            for (var x = -sqsize; x < (Game.RenderSize.Width / Game.Track.Zoom); x += sqsize)
            {
                var yv = new Vector2d(x + (Game.ScreenPosition.X - (Game.ScreenPosition.X % sqsize)), Game.ScreenPosition.Y);
                GL.Vertex2(yv);
                yv.Y += Game.RenderSize.Height / Game.Track.Zoom;
                GL.Vertex2(yv);
            }
            for (var y = -sqsize; y < (Game.RenderSize.Height / Game.Track.Zoom); y += sqsize)
            {
                var yv = new Vector2d(Game.ScreenPosition.X, y + (Game.ScreenPosition.Y - (Game.ScreenPosition.Y % sqsize)));
                GL.Vertex2(yv);
                yv.X += Game.RenderSize.Width / Game.Track.Zoom;
                GL.Vertex2(yv);
            }
            GL.End();
            GL.PopMatrix();
        }

        private static void DrawTexture(int tex, DoubleRect rect, Vector2d p1, Vector2d rotationAnchor, float opacity)
        {
            var angle = Angle.FromLine(p1, rotationAnchor);
            var offset = -(Game.ScreenPosition - p1);
            GL.PushMatrix();
            GL.Scale(Game.Track.Zoom, Game.Track.Zoom, 0);
            GL.Translate(offset.X, offset.Y, 0);
            GL.Rotate(angle.Degrees, 0, 0, 1);
            GL.Scale(0.5, 0.5, 0);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            StaticRenderer.DrawTexture(tex, rect, opacity);
            GL.PopMatrix();
        }
        #endregion Methods
    }
}