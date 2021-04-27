﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using static SnakeGame.Global;

namespace SnakeGame.SnakeLogic
{
    public class Field
    {
        public int Width, Height;
        public int?[] Debug;

        public List<Snake> Snakes;
        public List<Point> Apples;

        public Field(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;

            Snakes = new List<Snake>();
        }

        public void GenFood()
        {
            int alive = Snakes.Count(snake => snake.alive);
            int apples = Apples.Count;

            Random random = new Random();

            List<Point> freeSpace = new List<Point>();

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    freeSpace.Add(new Point(x, y));

            foreach (Snake snake in Snakes)
            {
                freeSpace.Remove(snake.HeadPos);
                foreach (Point p in snake.TailPoints)
                    freeSpace.Remove(p);
            }

            foreach (Point p in Apples)
                freeSpace.Remove(p);

            while (apples < alive + (alive > 1 ? 1 : 0) && freeSpace.Count > 0)
            {
                int a = random.Next(freeSpace.Count);

                Apples.Add(freeSpace[a]);

                freeSpace.RemoveAt(a);

                apples++;
            }
        }

        public void Update()
        {
            foreach (Snake snake in Snakes)
            {
                if (snake.bot)
                    snake.HeadDirection = snake.Bot(this);

                snake.Move(this);
            }

            GenFood();
        }

        public Grid Draw()
        {
            Grid grid = new Grid() { Width = Width, Height = Height };

            foreach (Point p in Apples)
                grid.Children.Add(new Border()
                {
                    Width = 1,
                    Height = 1,
                    Margin = new Thickness(p.X, p.Y, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = new Ellipse()
                    {
                        Height = 0.85,
                        Width = 0.85,
                        Fill = Application.Current.Resources["AppleBrush"] as Brush
                    }
                });

            foreach (Snake snake in Snakes)
                grid.Children.Add(snake.Render(this));

            if (DebugOverlay && Debug != null)
            {
                int x = 0, y = 0;
                foreach (int? i in Debug)
                {
                    grid.Children.Add(new Border()
                    {
                        Width = 1,
                        Height = 1,
                        Margin = new Thickness(x, y, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Child = new Grid()
                        {
                            Children =
                            {
                                new Ellipse()
                                {
                                    Height = 0.6,
                                    Width = 0.6,
                                    Opacity = 0.5,
                                    Fill = Application.Current.Resources["AltLowBrush"] as Brush
                                },
                                new TextBlock()
                                {
                                    VerticalAlignment = VerticalAlignment.Center,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    FontSize = 0.35,
                                    Text = i == int.MaxValue ? "#" : $"{i}",
                                    Foreground = Application.Current.Resources["BaseHighBrush"] as Brush
                                }
                            }
                        }
                    });
                    if (++x >= Width)
                    {
                        x = 0;
                        y++;
                    }
                }
            }

            return grid;
        }
    }

    public class Snake
    {
        private string name;
        public string Name
        {
            get => bot ? $"Бот {id}" : name;
            set => name = value;
        }

        public int id, score = 0;
        public string time = "00:00";
        public bool alive = true, bot = false;

        public Point HeadPos;
        public List<Point> TailPoints;
        public List<Vector> HeadDirs = new List<Vector>() { Vector.Right };

        public Vector HeadDirection
        {
            get
            {
                if (HeadDirs.Count > 1)
                    HeadDirs.RemoveAt(0);
                return HeadDirs[0];
            }

            set
            {
                Vector _vector = HeadDirs.Count > 0 ? HeadDirs[^1] : Vector.Zero;
                if (HeadDirs.Count < 3 && _vector != -value && _vector != value)
                    HeadDirs.Add(value);
            }
        }

        public Vector Bot(Field field)
        {
            if (alive)
            {
                int width = field.Width, height = field.Height;
                int up, left, down, right;

                up = left = down = right = int.MaxValue;

                int?[] Path = new int?[width * height];

                foreach (Point p in field.Apples)
                    Path[p.Y * width + p.X] = 0;

                foreach (Snake snake in field.Snakes)
                {
                    if (snake.TailPoints.Count > 0)
                        Path[snake.HeadPos.Y * width + snake.HeadPos.X] = int.MaxValue;
                    foreach (Point p in snake.TailPoints)
                        Path[p.Y * width + p.X] = int.MaxValue;
                }

                for (int i = 0; i < width * height; i++)
                    for (int j = 0; j < width * height; j++)
                        if (Path[j] == i)
                        {
                            if (j > width)
                                Path[j - width] ??= i + 1;

                            if (j % width > 0)
                                Path[j - 1] ??= i + 1;

                            if (j < width * (height - 1))
                                Path[j + width] ??= i + 1;

                            if (j % width < width - 1)
                                Path[j + 1] ??= i + 1;
                        }

                if (DebugOverlay)
                    field.Debug = Path;

                if (HeadPos.Y > 0)
                    up = Path[HeadPos.Y * width + HeadPos.X - width] ?? int.MaxValue >> 1;
                if (HeadPos.X > 0)
                    left = Path[HeadPos.Y * width + HeadPos.X - 1] ?? int.MaxValue >> 1;
                if (HeadPos.Y < height - 1)
                    down = Path[HeadPos.Y * width + HeadPos.X + width] ?? int.MaxValue >> 1;
                if (HeadPos.X < width - 1)
                    right = Path[HeadPos.Y * width + HeadPos.X + 1] ?? int.MaxValue >> 1;

                int min = new int[4] { up, left, down, right }.Min();

                if (up == min) return Vector.Up;
                if (left == min) return Vector.Left;
                if (down == min) return Vector.Down;
                if (right == min) return Vector.Right;
            }
            return Vector.Right;
        }

        public void Move(Field field)
        {
            Point Head = HeadPos + HeadDirection;

            if (TailPoints.Count > 0)
                TailPoints.RemoveAt(TailPoints.Count - 1);
            else HeadPos = new Point(-field.Width, -field.Height);

            if (alive)
            {
                alive = Head.X >= 0 && Head.X < field.Width &&
                        Head.Y >= 0 && Head.Y < field.Height;

                foreach (Snake snake in field.Snakes)
                    if (snake.TailPoints.Contains(Head) || snake.HeadPos == Head)
                    {
                        alive = false;
                        break;
                    }

                TailPoints.Insert(0, HeadPos);

                if (field.Apples.Contains(HeadPos))
                {
                    TailPoints.Add(TailPoints[^1]);
                    field.Apples.Remove(HeadPos);
                    score++;
                }

                if (alive)
                    HeadPos = Head;
            }
        }

        public Grid Render(Field field)
        {
            Grid grid = new Grid() { Width = field.Width, Height = field.Height };
            Brush snakeTailBrush = HsvToRgb(id * 360 / field.Snakes.Count + 195, 0.85, 1, 1);

            Polyline polyline = new Polyline()
            {
                Stroke = snakeTailBrush,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeThickness = 0.75
            };

            Vector v = new Vector(0, 0);
            for (int i = 0; i < TailPoints.Count - 2; i++)
            {
                Vector d = TailPoints[i + 1] - TailPoints[i];

                if (d != v)
                    polyline.Points.Add(new System.Windows.Point(
                            TailPoints[i].X + 0.5,
                            TailPoints[i].Y + 0.5));

                v = d;
            }


            if (TailPoints.Count > 1)
            {
                polyline.Points.Add(new System.Windows.Point(
                            TailPoints[^2].X + 0.5,
                            TailPoints[^2].Y + 0.5));

                Line end = new Line()
                {
                    X1 = TailPoints[^1].X + 0.5,
                    Y1 = TailPoints[^1].Y + 0.5,
                    X2 = TailPoints[^2].X + 0.5,
                    Y2 = TailPoints[^2].Y + 0.5,
                    Stroke = snakeTailBrush,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeThickness = 0.75
                };

                end.BeginAnimation(Line.X1Property,
                    new DoubleAnimation(end.X1, end.X2, GetrefreshTimeSpan()));
                end.BeginAnimation(Line.Y1Property,
                    new DoubleAnimation(end.Y1, end.Y2, GetrefreshTimeSpan()));

                grid.Children.Add(end);
            }
            else if (TailPoints.Count > 0)
            {
                Line end = new Line()
                {
                    X1 = TailPoints[^1].X + 0.5,
                    Y1 = TailPoints[^1].Y + 0.5,
                    X2 = HeadPos.X + 0.5,
                    Y2 = HeadPos.Y + 0.5,
                    Stroke = snakeTailBrush,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeThickness = 0.75
                };

                if (TailPoints.Count > 1)
                {
                    end.X2 = TailPoints[^2].X + 0.5;
                    end.Y2 = TailPoints[^2].Y + 0.5;
                }

                end.BeginAnimation(Line.X1Property,
                    new DoubleAnimation(end.X1, end.X2, GetrefreshTimeSpan()));
                end.BeginAnimation(Line.Y1Property,
                    new DoubleAnimation(end.Y1, end.Y2, GetrefreshTimeSpan()));

                grid.Children.Add(end);
            }

            Ellipse headEllipse = new Ellipse()
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 0.9,
                Height = 0.9,
                Fill = HsvToRgb(id * 360 / field.Snakes.Count + 195, 0.3, 1, 1),
            };

            if (TailPoints.Count > 0)
                headEllipse.Margin = new Thickness(TailPoints[0].X + 0.05, TailPoints[0].Y + 0.05, 0, 0);

            if (alive)
            {
                Line start = new Line()
                {
                    X2 = TailPoints[0].X + 0.5,
                    Y2 = TailPoints[0].Y + 0.5,
                    Stroke = snakeTailBrush,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeThickness = 0.75
                };

                start.BeginAnimation(Line.X1Property,
                    new DoubleAnimation(start.X2, HeadPos.X + 0.5, GetrefreshTimeSpan()));
                start.BeginAnimation(Line.Y1Property,
                    new DoubleAnimation(start.Y2, HeadPos.Y + 0.5, GetrefreshTimeSpan()));

                grid.Children.Add(start);

                headEllipse.BeginAnimation(FrameworkElement.MarginProperty,
                    new ThicknessAnimation(new Thickness(HeadPos.X + 0.05, HeadPos.Y + 0.05, 0, 0),
                    GetrefreshTimeSpan()));
            }
            else
            {
                polyline.Points.Insert(0, new System.Windows.Point(HeadPos.X + 0.5, HeadPos.Y + 0.5));

                headEllipse.Margin = new Thickness(HeadPos.X + 0.05, HeadPos.Y + 0.05, 0, 0);
            }

            grid.Children.Add(polyline);
            grid.Children.Add(headEllipse);

            return grid;
        }
    }
}
