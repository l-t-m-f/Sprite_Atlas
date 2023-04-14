# Sprite Atlas
SpriteAtlas using the C# SDL2 binding.

## Disclaimer

Early proof on concept inspired by the C Sprite Atlas of Parallel Realities.
Only works with .png.


## NuGet Package Dependency

Install Sayers's SDL2 bindings is the easiest way to get SDL2 on C#.
Search Sayers in the NuGet package manager in your IDE.

## How to use
1. Create an Images directory.
2. Place .pngs in the directory.
3. Run the demo, it will display a single texture which contains the individual images collaged together side-by-side.
4. (Texture size can be adjusted in the Atlas class.)
5. You can also draw individual images from the Atlas by their simple name (for example to acquire an SDL_Texture pointer of Images/abc.png, just draw get_atlas_image("abc")). This texture that will be acquired does not call any Loading function, since it just blits from the existing atlas surface! Big performance saver.

## Purpose

Make rendering faster with the simple SDL2 library by reducing the amount of textures loaded.
Useful for making a 2D game engine, for example.

Code by Émile Fréchette.
