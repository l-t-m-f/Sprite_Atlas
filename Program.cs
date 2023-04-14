#undef DEBUG

using System.Runtime.InteropServices;
using static SDL2.SDL;
using static SDL2.SDL_image;

namespace SpriteAtlas;

internal class Program
    {
        internal const string DIRECTORY_PATH = "Images";
        private const int WINDOW_W = 1280;
        private const int WINDOW_H = 720;
        private static IntPtr _window;
        internal static IntPtr renderer;

        static void Main(string[] args)
            {
                setup();
                Atlas atlas = new();
                const string search_pattern = "*.png";
                var file_names =
                    Directory.GetFiles(DIRECTORY_PATH, search_pattern);

                foreach (var file_name in file_names)
                    {
                        var file_path = Path.GetRelativePath(".", file_name);
#if DEBUG
                        Console.WriteLine(file_path);
#endif
                        atlas.surface_data[atlas.entries.Count] =
                            IMG_Load(file_path);
                        atlas.entries.Add(new AtlasEntry(file_path,
                            new SDL_Rect
                                    { x = 0, y = 0, w = 0, h = 0 }, false));
                    }

                Array.Resize(ref atlas.surface_data, atlas.entries.Count);
                Array.Sort(atlas.surface_data, atlas);

                for (var i = 0; i < atlas.surface_data.Length; i++)
                    {
                        SDL_QueryTexture(
                            SDL_CreateTextureFromSurface(renderer,
                                atlas.surface_data[i]), out _, out _, out int w,
                            out int h);

                        var rotated = false;
                        var found_node = Atlas.find_node(atlas.first, w, h);

                        if (found_node == null)
                            {
                                rotated = true;
                                found_node = Atlas.find_node(atlas.first, h, w);
                            }

                        if (found_node != null)
                            {
#if DEBUG
                                Console.WriteLine($"Node found for image #{i}");
#endif

                                //int rotations = 0;


                                if (rotated)
                                    {
                                        found_node.height = w;
                                        found_node.width = h;
                                        //rotations++;
                                    }

                                var dest = new SDL_Rect
                                    {
                                        x = found_node.x,
                                        y = found_node.y,
                                        w = w,
                                        h = h
                                    };

                                atlas.entries[i] =
                                    new AtlasEntry(atlas.entries[i].filename,
                                        dest, rotated);

                                if (rotated == false)
                                    {
                                        SDL_BlitSurface(atlas.surface_data[i],
                                            IntPtr.Zero,
                                            atlas.master_surface,
                                            ref dest);
                                    }
                                else
                                    {
                                        IntPtr result =
                                            blit_rotated(atlas.surface_data[i]);
                                        if (result != IntPtr.Zero)
                                            {
                                                SDL_BlitSurface(result,
                                                    IntPtr.Zero,
                                                    atlas.master_surface,
                                                    ref dest);
                                            }
                                    }
                            }

                        SDL_FreeSurface(atlas.surface_data[i]);
                    }

                // DEMO BEGINS
                
                // Create the master texture
                var master_texture =
                    SDL_CreateTextureFromSurface(renderer,
                        atlas.master_surface);
                // Extract a single image from the atlas (test)
                var test_extract =
                    atlas.get_atlas_image("1");
                // Query the texture for its width and height
                SDL_QueryTexture(test_extract, out _, out _,
                    out var extract_w,
                    out var extract_h);
                // Create the rectangles for the blit
                var dst_rect = new SDL_Rect
                        { x = 50, y = 50, w = extract_w, h = extract_h };
                var dst_rect2 = new SDL_Rect
                    {
                        x = 0, y = 0, w = Atlas.ATLAS_SIZE, h = Atlas.ATLAS_SIZE
                    };
                
#if DEBUG
                foreach (var ae in atlas.entries) {
                        Console.WriteLine(ae.filename);
                    }
#endif

                while (true)
                    {
                        while ((SDL_PollEvent(out var e)) != 0)
                            {
                                switch (e.type)
                                    {
                                        case SDL_EventType.SDL_QUIT:
                                            Environment.Exit(0);
                                            break;
                                        default:
                                            break;
                                    }
                            }

                        SDL_SetRenderDrawColor(renderer, 255, 0, 0, 255);
                        SDL_RenderClear(renderer);
                        SDL_RenderCopy(renderer,
                            master_texture,
                            0, ref dst_rect2);
                        SDL_RenderPresent(renderer);
                    }
            }

        private static void setup()
            {
                const uint sdl_flags = SDL_INIT_VIDEO;
                const SDL_WindowFlags window_flags =
                    SDL_WindowFlags.SDL_WINDOW_SHOWN;
                const SDL_RendererFlags renderer_flags =
                    SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                    SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC;
                const IMG_InitFlags img_flags = IMG_InitFlags.IMG_INIT_PNG;

                if (SDL_Init(sdl_flags) < 0)
                    {

                        SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            $"There was an issue starting SDL:\n{SDL_GetError()}!");
                    }

                _window = SDL_CreateWindow("SpriteAtlas Test",
                    SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED,
                    WINDOW_W, WINDOW_H, window_flags);

                if (_window == IntPtr.Zero)
                    {

                        SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            $"There was an issue creating the window:\n{SDL_GetError()}");
                    }

                renderer = SDL_CreateRenderer(_window, -1, renderer_flags);

                if (renderer == IntPtr.Zero)
                    {

                        SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            $"There was an issue creating the renderer:\n{SDL_GetError()}");
                    }

                if (IMG_Init(img_flags) != (int)img_flags)
                    {
                        SDL_LogError((int)SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                            $"There was an issue starting SDL_image:\n{SDL_GetError()}!");
                        
                    }
            }

        private static (byte r, byte g, byte b, byte a) get_pixel_color_values(
            IntPtr surface_ptr, int x, int y)
            {
                var pixel = get_pixel(surface_ptr, x, y);
                var r = (byte)(pixel & 0xFF);
                var g = (byte)((pixel >> 8) & 0xFF);
                var b = (byte)((pixel >> 16) & 0xFF);
                var a = (byte)((pixel >> 24) & 0xFF);
                return (r, g, b, a);
            }


        private static IntPtr blit_rotated(IntPtr source_surface_ptr)
            {
                var source_surface =
                    Marshal.PtrToStructure<SDL_Surface>(source_surface_ptr);

#if DEBUG
                Console.WriteLine(
                    $"Source surface dimensions: {source_surface.w}x{source_surface.h}");
#endif

                var dest_surface_ptr = SDL_CreateRGBSurfaceWithFormat(0,
                    source_surface.h, source_surface.w, 32,
                    SDL_PIXELFORMAT_ARGB8888);

                SDL_LockSurface(source_surface_ptr);
                SDL_LockSurface(dest_surface_ptr);

                for (var y = 0; y < source_surface.h; y++)
                    {
                        for (var x = 0; x < source_surface.w; x++)
                            {
                                var pixel = get_pixel(source_surface_ptr, x, y);
                                var (r, g, b, a) =
                                    get_pixel_color_values(source_surface_ptr,
                                        x, y);
#if DEBUG
                                Console.WriteLine(
                                    $"Source Pixel ({x}, {y}): R: {r} G: {g} B: {b} A: {a}");
#endif
                                var dest_x = source_surface.h - y - 1;
                                set_pixel(dest_surface_ptr, dest_x, x, pixel);

                                var (dest_r, dest_g, dest_b, dest_a) =
                                    get_pixel_color_values(dest_surface_ptr,
                                        dest_x, x);
#if DEBUG
                                Console.WriteLine(
                                    $"Dest Pixel ({dest_x}, {x}): R: {dest_r} G: {dest_g} B: {dest_b} A: {dest_a}");
#endif
                                set_pixel(dest_surface_ptr, dest_x, x, pixel);
                            }
                    }

                SDL_UnlockSurface(source_surface_ptr);
                SDL_UnlockSurface(dest_surface_ptr);

                return dest_surface_ptr;
            }

        private static uint get_pixel(IntPtr surface_ptr, int x, int y)
            {
                var surface =
                    Marshal.PtrToStructure<SDL_Surface>(surface_ptr);
                const int bytes_per_pixel = 4; //RGBA8888
                var pixel = surface.pixels + y * surface.pitch +
                            x * bytes_per_pixel;
                return (uint)Marshal.ReadInt32(pixel);
            }

        private static void set_pixel(IntPtr surface_ptr, int x, int y,
            uint new_pixel)
            {
                var surface =
                    Marshal.PtrToStructure<SDL_Surface>(surface_ptr);
                const int bytes_per_pixel = 4; //RGBA8888
                var pixel = surface.pixels + y * surface.pitch +
                            x * bytes_per_pixel;

                Marshal.WriteInt32(pixel, (int)new_pixel);
            }
    }