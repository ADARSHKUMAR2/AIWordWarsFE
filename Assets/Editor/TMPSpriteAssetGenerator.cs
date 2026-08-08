using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore; 

public class TMPSpriteAssetRenamer : EditorWindow
{
    private TMP_SpriteAsset spriteAsset;

    private readonly string[] spriteNames =
    {
        // Row 1
        "coin", "gem", "xp", "trophy", "crown", "fire", "energy", "heart",
        // Row 2
        "timer", "target", "puzzle", "letter", "shuffle", "hint", "reward", "dice",
        // Row 3
        "friends", "globe", "battle", "leaderboard", "home", "profile", "shop", "settings",
        // Row 4
        "plus", "close", "check", "black_heart", "lock", "share", "notification", "chat",
        // Row 5
        "celebration", "gift", "calendar", "badge", "mail", "shield", "search", "bookmark"
    };

    [MenuItem("AI Word Wars/Rename TMP Sprites")]
    public static void ShowWindow()
    {
        GetWindow<TMPSpriteAssetRenamer>("TMP Sprite Renamer");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("AI Word Wars - TMP Sprite Renamer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        spriteAsset = (TMP_SpriteAsset)EditorGUILayout.ObjectField(
            "Sprite Asset",
            spriteAsset,
            typeof(TMP_SpriteAsset),
            false
        );

        GUILayout.Space(10);

        if (spriteAsset == null)
        {
            EditorGUILayout.HelpBox("Select your TMP Sprite Asset first. (Right-click your sprite sheet -> Create -> TextMeshPro -> Sprite Asset, then drag it here!)", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("Rename Sprites", GUILayout.Height(40)))
        {
            RenameSprites();
        }
    }

    private void RenameSprites()
    {
        if (spriteAsset == null) return;

        // Extract characters and find their glyph rects to sort them
        List<TMP_SpriteCharacter> charTable = spriteAsset.spriteCharacterTable;
        List<TMP_SpriteGlyph> glyphTable = spriteAsset.spriteGlyphTable;

        if (charTable == null || glyphTable == null)
        {
            Debug.LogError("Could not access character or glyph tables.");
            return;
        }

        // We want to sort characters by their glyph's Y (descending) and X (ascending)
        charTable.Sort((a, b) =>
        {
            TMP_SpriteGlyph glyphA = glyphTable.Find(g => g.index == a.glyphIndex);
            TMP_SpriteGlyph glyphB = glyphTable.Find(g => g.index == b.glyphIndex);

            if (glyphA == null || glyphB == null) return 0;

            // Sort by Y descending (top to bottom)
            int yComp = glyphB.glyphRect.y.CompareTo(glyphA.glyphRect.y);
            
            // Floating point math might cause slight deviations, so we use a margin of 5 pixels
            if (Mathf.Abs(glyphB.glyphRect.y - glyphA.glyphRect.y) > 5) return yComp;
            
            // Sort by X ascending (left to right)
            return glyphA.glyphRect.x.CompareTo(glyphB.glyphRect.x);
        });

        // Now rename them based on the array
        int renameCount = 0;
        for (int i = 0; i < charTable.Count && i < spriteNames.Length; i++)
        {
            charTable[i].name = spriteNames[i];
            renameCount++;
        }

        // Apply changes
        spriteAsset.UpdateLookupTables();
        EditorUtility.SetDirty(spriteAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Successfully renamed {renameCount} sprites in {spriteAsset.name}!");
    }
}