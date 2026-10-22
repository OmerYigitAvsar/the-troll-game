# Pixel World — Level 1 preview

Generated with the built-in image_gen tool. The user's reference image was not used as an input or copied.
Original generated PNGs are retained. Unity imports these as Point-filtered, uncompressed sprites with a 64-pixel maximum texture size for coarse pixel display; transparent alpha is retained for clouds and spikes.

Use TrickRun > Preview Level 1 - Pixel World outside Play mode, then Ctrl+S to keep or Ctrl+Z to undo. The tool does not save the scene automatically, rebuild levels, edit colliders, or apply shared prefab changes.
Cloud movement is stored in AnimationClip and AnimatorController assets, with no new runtime script.

## Generation prompts

### Ground
+
Use case: stylized-concept. Asset type: original Unity 2D tileable game texture. Create a square front-facing soil tile filling the ENTIRE canvas edge to edge, no margin, no background. Coarse authentic pixel art on an exact 32 by 32 logical pixel grid upscaled with nearest-neighbor. Warm muted brown soil with sparse larger clusters of darker earth and ochre pebbles. Only the TOP THREE logical pixel rows contain short muted moss-green grass, 90 percent brown soil. Flat horizontal top, no tall grass blades. Seamlessly repeat horizontally. Cute subdued world, medium value, not bright. No perspective, bevel, lighting gradients, antialiasing, text, watermark, outline around tile, or copied stock assets. Brown soil extends to left right and bottom edges.

### Spike
+
Use case: stylized-concept. Asset type: single original 2D platformer hazard sprite on genuinely TRANSPARENT background. Exactly one upright triangular steel spike, symmetric narrow sharp triangle with a flat base, no pedestal. Shape extends from top center almost to full canvas height and full width along bottom. Authentic coarse pixel art drawn on a 32 by 32 logical grid enlarged nearest-neighbor; visible stair-step triangle edges. Dark blue-grey metal outline, muted silver center, one slim cool highlight on left facet, darker right facet, only 5 flat colors. Cute pixel game, clearly dangerous. No glow, no gradients, no smoothing, no shadow outside triangle, no additional objects, no text or watermarks.

### Wood
+
Use case: stylized-concept. Asset type: original seamless Unity 2D wood platform tile. Square image filled EDGE TO EDGE with side-on warm muted brown timber, two horizontal planks with sparse dark horizontal pixel wood-grain and one understated small knot. Authentic coarse pixel art on 32x32 logical pixels enlarged nearest-neighbor. Matte low saturation caramel tan highlights and deep brown groove. Repeatable horizontally and vertically, no outer border, no transparent padding, no grass, no perspective, no bevelled 3D render, no gradients, no antialiasing, no text or watermarks. This is an opaque texture tile, not a floating object illustration.

### Cloud
+
Use case: stylized-concept. Asset type: one original cloud sprite for a 2D pixel platformer, genuinely TRANSPARENT background. A single wide low puffy cloud with flat-ish bottom and three unequal rounded lobes described entirely by large stair-step square pixels. Authentic 48x24 logical pixel art enlarged nearest-neighbor. Muted dusty lavender-grey highlights and blue-grey underside, 4 flat shades, NO pure white. Cute gentle dusk cloud, horizontal silhouette fills most of canvas, no other clouds, no landscape, no rain, no faces, no glow, no smooth curves, no gradients, no text or watermark.

