﻿using System.Diagnostics;
using System.IO;
using Logic;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Barricade.Data
{
    public class Loader
    {
        public Dictionary<char, Speler> Spelers { get; private set; }
        public IVeld[,] Kaart { get; private set; }
        public List<Connection> Connecties { get; private set; }

        public Loader(String[] lines)
        {
            // Alle informatie over bijzondere vakjes ophalen
            var lastList = from line in lines
                           where line.StartsWith("*") && line.Contains(":") && !line.EndsWith(":")
                           select line;

            var uitzonderingen = lastList.ToDictionary(
                line => line.Trim('*').Split(':')[0][0],
                line => line.Split(':')[1]);

            int firstX,firstY;
            int lastX,lastY;
            int isXeven,isYeven;
            CalculateSize(lines, out firstX, out firstY, out lastX, out lastY, out isXeven, out isYeven);
            var height = (int) Math.Ceiling(((decimal) (lastY - firstY + 1)/2));
            var width = (lastX - firstX)/4 + 1;

            Spelers = new Dictionary<char, Speler>();
            Connecties = new List<Connection>();
            Kaart = new IVeld[height, width];


            var getX = new Func<int, int>(x => (x - firstX) / 4);
            var getY = new Func<int, int>(y => (int)Math.Ceiling(((decimal)(y - firstY + 1) / 2)) - 1);

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length < 2) continue;

                var line = lines[i];

                var isDorp = (line[0] + line[1] + "").Contains("D");
                var isBarricadeVrij = !(line[0] + line[1] + "").Contains("-");

                for (var j = firstX; j < line.Length; j++)
                {
                    var letter = line[j];
                    if (new[] {'<', '(', '[', '{'}.Contains(letter))
                    {
                        var letters = line.Substring(j, 3);

                         Kaart[getY(i), getX(j+1)] = ParseBlock(letters, isBarricadeVrij, isDorp, uitzonderingen);

                        j += 2;
                    } 
                    else if (letter == '-')
                    {
                        var pos1 = new Position(getX(j - 2), getY(i));
                        var pos2 = new Position(getX(j + 2), getY(i));
                        Connecties.Add(new Connection(pos1, pos2));
                    }
                    else if (letter == '|')
                    {
                        if (i%2 != isYeven)
                        {
                            var pos1 = new Position(getX(j), getY(i - 1));
                            var pos2 = new Position(getX(j), getY(i + 1));

                            Connecties.Add(new Connection(pos1, pos2));
                        }
                    }
                }
            }

            /**
             * Hier alle nodes koppelen
             */
            foreach (var connectie in Connecties)
            {
                var first = Kaart[connectie.Item1.Y, connectie.Item1.X];
                var second = Kaart[connectie.Item2.Y, connectie.Item2.X];

                if (first != null && second == null)
                {
                    // Kijk of de code verticaal of horizontaal moet
                    if (connectie.Item1.X == connectie.Item2.X)
                    {
                        for (var i = connectie.Item2.Y; i < Kaart.GetLength(0); i++)
                        {
                            second = Kaart[i, connectie.Item2.X];
                            if (second != null) break;
                        }
                    }
                    else if (connectie.Item1.Y == connectie.Item2.Y)
                    {
                        for (var i = connectie.Item2.X; i < Kaart.GetLength(1); i++)
                        {
                            second = Kaart[connectie.Item2.Y, i];
                            if (second != null) break;
                        }
                    }
                }
                if (first == null || second == null) continue;

                first.Buren.Add(second);
                second.Buren.Add(first);
            }
        }

        private IVeld ParseBlock(string letters, bool isBarricadeVrij, bool isDorp, IReadOnlyDictionary<char, string> uitzonderingen)
        {
            IVeld veld = null;
            if (letters[0] == '<' && letters[2] == '>')
            {
                if(letters[1] == ' ')
                    veld = new Finishveld();
                else
                {
                    if (uitzonderingen.ContainsKey(letters[1]))
                    {
                        var uitzondering = uitzonderingen[letters[1]];
                        if (uitzondering.StartsWith("BOS"))
                        {
                            veld = new Bos();

                            var players = uitzondering.Split(',')[1];
                            foreach (var player in players)
                            {
                                CreatePlayer(player, Spelers, veld);
                            }
                        }
                        else if (uitzondering.StartsWith("START"))
                        {
                            if (!uitzondering.Contains(",") || uitzondering.EndsWith(","))
                            {
                                throw new ParserException("Uitzondering '" + letters[1] + "' (START), heeft geen speler");
                            }
                            veld = new Startveld();

                            var players = uitzondering.Split(',')[1];
                            foreach (var player in players)
                            {
                                CreatePlayer(player, Spelers, veld);
                            }
                        }
                        else
                        {
                            throw new ParserException("Uitzondering '" + letters[1] + "' snap ik niet.");
                        }
                    }
                }

                return veld;
            }

            if (letters[0] == '(' && letters[2] == ')')
            {
                veld = new Veld();
            }
            else if (letters[0] == '[' && letters[2] == ']')
            {
                veld = new Veld();
                //TODO: veld rood maken omdat er een barricade opstaat
            }
            else if (letters[0] == '{' && letters[2] == '}')
            {
                veld = new Rustveld();
            }
            else
            {
                throw new ParserException("Dit veld ken ik niet");
            }
            veld.IsDorp = isDorp;
            /**
             * Kijken of er een barricade op mag komen.
             */
            if (veld is Veld)
            {
                var barricadeVeld = veld as Veld;
                barricadeVeld.MagBarricade = isBarricadeVrij;
                if (isBarricadeVrij && letters[1] == '*')
                {
                    var barricade = new Logic.Barricade();
                    barricadeVeld.Barricade = barricade;
                }
            }

            /**
             * Kijken of er een speler op staat
             */
            if (letters[1] != '*' && letters[1] != ' ')
            {
                CreatePlayer(letters[1], Spelers, veld);
            }

            return veld;
        }

        private static void CreatePlayer(char letter, Dictionary<char, Speler> spelers, IVeld veld)
        {
            if (!spelers.ContainsKey(letter))
            {
                spelers[letter] = new Speler(letter);
            }
            var pion = new Pion(spelers[letter]) {IVeld = veld};
            if (!veld.Pionen.Contains(pion))
                veld.Pionen.Add(pion);
            spelers[letter].Pionen.Add(pion);
        }

        private static void CalculateSize(IList<string> lines, out int firstX, out int firstY, out int lastX, out int lastY,
                                             out int isXeven, out int isYeven)
        {
            // Min, max values, voor uitrekenen van breedte en lengte
            firstX = int.MaxValue;
            firstY = int.MaxValue;
            lastX = int.MinValue;
            lastY = int.MinValue;

            // Snelle controle naar spelgrootte en hoe de vakjes staan
            for (var i = 0; i < lines.Count; i++)
            {
                for (var j = 0; j < lines[i].Length; j++)
                {
                    if (!new[] {'<', '(', '[', '{'}.Contains(lines[i][j])) continue;

                    firstX = Math.Min(firstX, j);
                    lastX = Math.Max(lastX, j + 3);

                    firstY = Math.Min(firstY, i);
                    lastY = Math.Max(lastY, i);
                }
            }
            // Trucje om te kijken waar het middenpunt van een vakje zit
            isXeven = (lastX - firstX - 1)%2;
            isYeven = firstY%2;
        }

        public Loader(TextReader file)
        {
            throw new NotImplementedException();
        }


        public IVeld[,] ToArray()
        {
            return Kaart;
        }

        public class Connection : Tuple<Position, Position>
        {
            public Connection(Position item1, Position item2) : base(item1, item2)
            {
            }
        }

        public class Position : Tuple<int, int>
        {
            public Position(int item1, int item2) : base(item1, item2)
            {
            }

            public int X { get { return Item1; } }
            public int Y { get { return Item2; } }
        }

        public class Point
        {
            public Point(Position a1, IVeld a2)
            {
                Locatie = a1;
                Veld = a2;
            }
            public IVeld Veld { get; set; }
            public Position Locatie { get; set; }
        }
    }

    public class ParserException : Exception
    {
        public ParserException(string s)
            : base(s)
        {

        }

        public ParserException(int linenr, int letternr, string message) 
            : base ("[regel "+linenr+", karakter "+letternr+"] "+message)
        {
        }
    }
}

