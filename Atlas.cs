#undef DEBUG

using static SDL2.SDL;

namespace SpriteAtlas;

/**
 * The atlas class is responsible for creating the atlas and managing the images
 * in it. It also provides methods for extracting images from the atlas.
 * It has the ability to sort images based on their height, which is
 * encapsulated by the IComparer interface.
 */
internal class Atlas : IComparer<IntPtr>
    {
        internal const int ATLAS_SIZE = 300;
        private const int PADDING = 4;

        public List<AtlasEntry> entries { get; }

        internal IntPtr[] surface_data = new IntPtr[50]; // surfaces
        public readonly AtlasNode first;
        private readonly LinkedList<AtlasNode> _nodes = new();
        internal readonly IntPtr master_surface;

        public Atlas()
            {
                entries = new List<AtlasEntry>();
                var root_node = new AtlasNode(0, 0, ATLAS_SIZE, ATLAS_SIZE);
                _nodes.AddLast(root_node);
                master_surface = SDL_CreateRGBSurfaceWithFormat(0, ATLAS_SIZE,
                    ATLAS_SIZE,
                    32,
                    SDL_PIXELFORMAT_ARGB8888); // used to be RGBA8888
                first = root_node;
            }

        // IComparer Interface
        /// <summary>
        /// This implementation of IComparer is used to sort the images files according
        /// to their height.
        /// </summary>
        /// <param name="surface_a"></param>
        /// <param name="surface_b"></param>
        /// <returns></returns>
        public int Compare(IntPtr surface_a, IntPtr surface_b)
            {
                if (SDL_QueryTexture(
                        SDL_CreateTextureFromSurface(Program.renderer,
                            surface_a),
                        out _,
                        out _, out var height_x, out _) < 0)
                    {

                        SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            $"There was an issue querying the texture \n{SDL_GetError()}");
                        return 0;
                    }

                if (SDL_QueryTexture(
                        SDL_CreateTextureFromSurface(Program.renderer,
                            surface_b),
                        out _,
                        out _, out var height_y, out _) < 0)
                    {

                        SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            $"There was an issue querying the texture \n{SDL_GetError()}");
                        return 0;
                    }

                return height_y.CompareTo(height_x);
                // Descending order
            }

        // Atlas Tree Methods

        /// <summary>
        /// Recover a SDL_Texture pointer based on the filename. For this to
        /// work, the image must have been added to the atlas first. If the
        /// image is within the DIRECTORY_PATH, then the filename should be
        /// valid. The filename allows us to recover an AtlasEntry which has
        /// the SDL_Rect information required to extract the image from the
        /// master surface.
        /// </summary>
        /// <param name="short_filename"></param>
        /// <returns></returns>
        public IntPtr get_atlas_image(string short_filename)
            {
                var full_filename =
                    $"{Program.DIRECTORY_PATH}\\{short_filename}.png";
                AtlasEntry? entry = null;
                foreach (var t in entries)
                    {
                        if (t.filename != full_filename) continue;
                        entry = t;
                        break;
                    }

                if (entry is null)
                    {
                        return 0;
                    }

                var extraction_rectangle = entry.rectangle;

                // Create a new surface to hold the extracted image
                var extracted_surface = SDL_CreateRGBSurfaceWithFormat(0,
                    extraction_rectangle.w, extraction_rectangle.h, 32,
                    SDL_PIXELFORMAT_ARGB8888); // used to be RGBA8888
                if (extracted_surface == IntPtr.Zero)
                    {

                        SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            $"There was an issue creating the extracted surface:\n{SDL_GetError()}");
                        return IntPtr.Zero;
                    }

                // Extract the image from the MasterSurface using the extraction rectangle
                if (SDL_BlitSurface(master_surface, ref extraction_rectangle,
                        extracted_surface, IntPtr.Zero) != 0)
                    {

                        SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            $"There was an issue extracting the image:\n{SDL_GetError()}");
                        SDL_FreeSurface(extracted_surface);
                        return IntPtr.Zero;
                    }

                var final_texture =
                    SDL_CreateTextureFromSurface(Program.renderer,
                        extracted_surface);

                SDL_FreeSurface(extracted_surface);

                return final_texture;
            }

        /// <summary>
        /// This function attempts to find a suitable node in the atlas tree.
        /// If a suitable node is found, it is marked as used and the children
        /// nodes are created. If no suitable node is found, then the function
        /// returns null.
        /// </summary>
        /// <param name="parent_node"></param>
        /// <param name="required_w">Width dimension required for the node.</param>
        /// <param name="required_h">Height dimension required for the node.</param>
        /// <returns></returns>
        public static AtlasNode? find_node(AtlasNode parent_node,
            int required_w, int required_h)
            {
                if (parent_node.used)
                    {
                        if (parent_node.children == null)
                            {
#if DEBUG
                                Console.WriteLine(
                                    $"Node ({parent_node.x}, {parent_node.y}, " +
                                    $"{parent_node.width}, {parent_node.height}) is used but has no children.");
#endif
                                return null;
                            }

                        var node = find_node(parent_node.children[0],
                                       required_w, required_h) ??
                                   find_node(parent_node.children[1],
                                       required_w, required_h);
                        return node;
                    }
                else if (required_w <= parent_node.width &&
                         required_h <= parent_node.height)
                    {
#if DEBUG
                        Console.WriteLine(
                            $"Found a suitable node at ({parent_node.x}, {parent_node.y}, " +
                            $"{parent_node.width}, {parent_node.height}) for dimensions ({required_w}, {required_h})");
#endif
                        split_node(parent_node, required_w, required_h);
                        return parent_node;
                    }

#if DEBUG
                Console.WriteLine($"Node ({parent_node.x}, {parent_node.y}, " +
                                  $"{parent_node.width}, {parent_node.height}) is too small for dimensions ({required_w}, {required_h})");
#endif
                return null;
            }

        /// <summary>
        /// Split a node into two children nodes. The node is marked as used.
        /// </summary>
        /// <param name="parent_node"></param>
        /// <param name="used_w"></param>
        /// <param name="used_h"></param>
        private static void split_node(AtlasNode parent_node, int used_w,
            int used_h)
            {
                parent_node.used = true;

                var width_padding = (parent_node.width - used_w >= PADDING)
                    ? PADDING
                    : 0;
                var height_padding = (parent_node.height - used_h >= PADDING)
                    ? PADDING
                    : 0;

                if (parent_node.width - used_w - width_padding < 0 ||
                    parent_node.height - used_h - height_padding < 0)
                    {

                        SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            $"Invalid split at Node ({parent_node.x}, {parent_node.y}, " +
                            $"{parent_node.width}, {parent_node.height}) with dimensions ({used_w}, {used_h})");
                        return;
                    }

                parent_node.children = new AtlasNode[2];
                parent_node.children[0] =
                    new AtlasNode(parent_node.x + used_w + width_padding,
                        parent_node.y,
                        parent_node.width - used_w - width_padding, used_h);
                parent_node.children[1] =
                    new AtlasNode(parent_node.x,
                        parent_node.y + used_h + height_padding,
                        parent_node.width,
                        parent_node.height - used_h - height_padding);
            }
    }

/**
 * A node in the atlas tree.
 * As the tree is created, more nodes are made by splitting the last node
 */
public class AtlasNode
    {
        public bool used { get; set; }
        public int x { get; }
        public int y { get; }
        public int width { get; set; }

        public int height { get; set; }

        // Children can be null or have valid elements
        public AtlasNode[]? children { get; set; }

        public AtlasNode(int x, int y, int width, int height)
            {
                this.x = x;
                this.y = y;
                this.width = width;
                this.height = height;
                this.children = null;
            }
    }

/**
 * Entries represent finalized images in the atlas with their filename,
 * so they can be identified easily, drawing rectangle, and a rotation flag.
 */
internal class AtlasEntry
    {
        public string filename { get; }
        public SDL_Rect rectangle { get; set; }
        public bool rotated { get; set; }

        public AtlasEntry(string filename, SDL_Rect rectangle, bool rotated)
            {
                this.filename = filename;
                this.rectangle = rectangle;
                this.rotated = rotated;
            }
    }