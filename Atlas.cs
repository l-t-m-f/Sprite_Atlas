﻿using static SDL2.SDL;

namespace SpriteAtlas
{
public class Node
    {
        public bool Used { get; set; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Node[]? Children { get; set; }

        public Node(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                Children = null;
            }
    }

internal class AtlasEntry
    {
        public string Filename { get; }
        public SDL_Rect Rectangle { get; set; }
        public bool Rotated { get; set; }

        public AtlasEntry(string filename, SDL_Rect rectangle, bool rotated)
            {
                Filename = filename;
                Rectangle = rectangle;
                Rotated = rotated;
            }
    }

internal class Atlas : IComparer<nint>
    {
        internal const int ATLAS_SIZE = 300;
        private const int PADDING = 2;
        
        public List<AtlasEntry> Entries { get; }

        internal nint[] SurfaceData = new nint[50]; // surfaces
        public readonly Node First;
        private readonly LinkedList<Node> _nodes = new();
        internal readonly IntPtr MasterSurface;

        public Atlas()
            {
                Entries = new List<AtlasEntry>();
                var root_node = new Node(0, 0, ATLAS_SIZE, ATLAS_SIZE);
                _nodes.AddLast(root_node);
                MasterSurface =
                    SDL_CreateRGBSurfaceWithFormat(0, ATLAS_SIZE, ATLAS_SIZE,
                        32,
                        SDL_PIXELFORMAT_RGBA8888);
                First = root_node;
            }

        public nint GetAtlasImage(string filename)
            {
                AtlasEntry? entry = null;
                foreach (var t in Entries)
                    {
                        if (t is null) return 0;
                        if (t.Filename != filename) continue;
                        entry = t;
                        break;
                    }

                if (entry is null)
                    {
                        return 0;
                    }

                var extraction_rectangle = entry.Rectangle;

                // Create a new surface to hold the extracted image
                var extracted_surface = SDL_CreateRGBSurfaceWithFormat(0,
                    extraction_rectangle.w, extraction_rectangle.h, 32,
                    SDL_PIXELFORMAT_RGBA8888);
                if (extracted_surface == IntPtr.Zero)
                    {
                        Console.WriteLine(
                            $"There was an issue creating the extracted surface:\n{SDL_GetError()}");
                        return IntPtr.Zero;
                    }

                // Extract the image from the MasterSurface using the extraction rectangle
                if (SDL_BlitSurface(MasterSurface, ref extraction_rectangle,
                        extracted_surface, IntPtr.Zero) != 0)
                    {
                        Console.WriteLine(
                            $"There was an issue extracting the image:\n{SDL_GetError()}");
                        SDL_FreeSurface(extracted_surface);
                        return IntPtr.Zero;
                    }

                var final_texture =
                    SDL_CreateTextureFromSurface(Program.Renderer,
                        extracted_surface);

                SDL_FreeSurface(extracted_surface);

                return final_texture;
            }

        public int Compare(nint x, nint y)
            {
                if (SDL_QueryTexture(
                        SDL_CreateTextureFromSurface(Program.Renderer, x),
                        out _,
                        out _, out var height_x, out _) < 0)
                    {
                        Console.WriteLine(
                            $"There was an issue querying the texture \n{SDL_GetError()}");
                        return 0;
                    }

                if (SDL_QueryTexture(
                        SDL_CreateTextureFromSurface(Program.Renderer, y),
                        out _,
                        out _, out var height_y, out _) >= 0)
                    {
                        return height_y.CompareTo(height_x);
                    }

                Console.WriteLine(
                    $"There was an issue querying the texture \n{SDL_GetError()}");
                return 0;

                // Descending order
            }

        public static Node? FindNode(Node root, int w, int h)
            {
                if (root.Used)
                    {
                        if (root.Children == null)
                            {
                                Console.WriteLine(
                                    $"Node ({root.X}, {root.Y}, " +
                                    $"{root.Width}, {root.Height}) is used but has no children.");
                
                                return null;
                            }
                
                        var node = FindNode(root.Children[0], w, h) ??
                                   FindNode(root.Children[1], w, h);
                        return node;
                    }
                else if (w <= root.Width && h <= root.Height)
                    {
                        Console.WriteLine($"Found a suitable node at ({root.X}, {root.Y}, " +
                                          $"{root.Width}, {root.Height}) for dimensions ({w}, {h})");
                
                        SplitNode(root, w, h);
                        return root;
                    }
                
                Console.WriteLine($"Node ({root.X}, {root.Y}, " +
                                  $"{root.Width}, {root.Height}) is too small for dimensions ({w}, {h})");
                
                return null;
                
                
                
            }

        private static void SplitNode(Node node, int w, int h)
            {
                node.Used = true;

                int widthPadding = (node.Width - w >= PADDING) ? PADDING : 0;
                int heightPadding = (node.Height - h >= PADDING) ? PADDING : 0;
                
                if (node.Width - w - widthPadding < 0 || node.Height - h - heightPadding < 0)
                    {
                        Console.WriteLine(
                            $"Invalid split at Node ({node.X}, {node.Y}, " +
                            $"{node.Width}, {node.Height}) with dimensions ({w}, {h})");
                        return;
                    }

                node.Children = new Node[2];
                node.Children[0] =
                    new Node(node.X + w + widthPadding, node.Y,
                        node.Width - w - widthPadding, h);
                node.Children[1] =
                    new Node(node.X, node.Y + h + heightPadding,
                        node.Width, node.Height - h - heightPadding);
            }
    }
}