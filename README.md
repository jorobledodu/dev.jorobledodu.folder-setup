# Folder Setup Wizard (by jorobledodu)

A Unity editor tool to **create or delete folder structures** from simple text input.  
Ideal for initializing projects with a clean and consistent hierarchy.

---

## Features

- Create and delete folder structures directly from the Unity Editor.  
- The **Delete Folders** button includes double‑confirmation and content detection.  
- Optional creation of `.gitkeep` files in empty folders.  
- Supports multiple input formats:  
  - Simple bullets + indentation (recommended)  
  - One path per line  
  - Classic tree format (├ └ │)  
  - JSON format (for scripting/automation)  
- Automatic file detection by extension: `.unity`, `.asset`, `.prefab`, etc.  
- Compatible with Unity 6 (URP, HDRP or Built‑in pipelines).

---

## How to Write Folder Structures

You can define the structure in several ways:

### 1. Simple bullets + indentation (recommended)

```
Assets
- Art
  - Sprites
  - Atlases
- Scenes
  - 01_Prototype.unity
  - 02_LevelEditor.unity
```

### 2. One path per line

```
Assets/Art/Sprites
Assets/Scenes/01_Prototype.unity
```

### 3. Classic box‑drawing tree (optional)

```
Assets
 ├ Art
 │  └ Sprites
 └ Scenes
    └ 01_Prototype.unity
```

### 4. JSON (for automation or scripting)

```json
{
  "name": "Assets",
  "children": [
    { "name": "Art", "children": [ { "name": "Sprites" } ] }
  ]
}
```

---

## Installation (UPM)

1. Open **Window → Package Manager** in Unity.  
2. Click the **+** button and choose **Add package from git URL…**  
3. Paste this URL:

```
https://github.com/jorobledodu/dev.jorobledodu.folder-setup.git#v1.0.0
```

---

## Usage

1. Open **Tools → Folder Setup Wizard** in the Unity Editor.  
2. Paste your folder‑structure text (or JSON) into the text area.  
3. Optionally toggle **Create .gitkeep in empty folders** if you want `.gitkeep` files.  
4. Click one of the main buttons:  
   - **Create Folders & Files** → Generates the folders + files.  
   - **Delete Folders** → Deletes the folders specified (with confirmation + content detection).  
5. Check the Unity Console/log for any errors or confirmation messages.

---

## Example Output

If you use the example structure above, you might get:

```
Assets/
 ├ Art/
 │  ├ Sprites/
 │  └ Atlases/
 ├ Scenes/
 │  ├ 01_Prototype.unity
 │  └ 02_LevelEditor.unity
 └ Scripts/
    └ Player/
```

---

## License

This project is licensed under the **MIT License** © 2025 jorobledodu.
