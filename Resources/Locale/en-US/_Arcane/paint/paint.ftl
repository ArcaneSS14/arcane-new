guide-entry-painting = Painting

paint-window-title = Canvas
paint-window-tools = Tools
paint-window-brush = Brush style
paint-window-size = Size
paint-window-size-value = {$size}px
paint-window-color = Colour
paint-window-no-tool = Hold a crayon or paintbrush to paint.
paint-window-undo = Undo your last stroke (Ctrl+Z)
paint-window-finish = Finish
paint-window-title-placeholder = Title
paint-window-finish-button = Finish and sign
paint-window-finish-confirm = Sign? It can't be changed after.
paint-window-finished = [bold]{$title}[/bold]
    Signed by {$artist}

paint-tool-brush = Brush
paint-tool-eraser = Eraser
paint-tool-line = Line
paint-tool-rectangle = Rectangle
paint-tool-ellipse = Ellipse
paint-tool-picker = Colour picker

paint-style-round = Round
paint-style-square = Square
paint-style-spray = Spray
paint-style-calligraphy = Calligraphy

paint-tool-empty = {CAPITALIZE(THE($tool))} is all used up.

paint-untitled = Untitled
paint-unknown-artist = an unknown artist
paint-finished-name = painting "{$title}"
paint-copy-name = copy of "{$title}"
paint-copy-artist = {$artist} (copy)
paint-finished-popup = You sign "{$title}".
paint-examine-finished = It's titled [bold]{$title}[/bold], signed by {$artist}.
paint-fax-use-photocopier = Fax machines can't handle paintings. Use a photocopier.

paint-slot-canvas = canvas

paint-frame-unfinished = Finish and sign the painting before framing it.
paint-frame-already-framed = That painting already has a frame.
paint-frame-applied = You frame {THE($painting)}.

paint-log-view-button = View painting
paint-log-viewer-title = Painting {$token}
paint-log-viewer-missing = This painting could not be found. It may not have been saved to the database yet.

cmd-paintloadurl-desc = Downloads an image and stretches it onto a paintable surface, such as a canvas.
cmd-paintloadurl-help = Usage: paintloadurl <entity uid> <http(s) url>
cmd-paintloadurl-hint-target = <entity uid>
cmd-paintloadurl-hint-url = <http(s) url>
cmd-paintloadurl-bad-target = {$target} is not a paintable surface.
cmd-paintloadurl-bad-url = The URL must be an absolute http or https address.
cmd-paintloadurl-downloading = Downloading {$url}...
cmd-paintloadurl-too-large = The image is larger than 8 MB.
cmd-paintloadurl-download-failed = Download failed: {$error}
cmd-paintloadurl-bad-image = Could not read the image: {$error}
cmd-paintloadurl-success = Loaded the image onto {$target}.

reagent-desc-paint = Thick, brightly coloured paint. Pour it into a paint can to paint with it.
reagent-name-paint-red = red paint
reagent-name-paint-yellow = yellow paint
reagent-name-paint-green = green paint
reagent-name-paint-blue = blue paint
reagent-name-paint-violet = violet paint
reagent-name-paint-black = black paint
reagent-name-paint-white = white paint

photocopier-verb-copy = Copy painting
photocopier-busy = It's already copying.
photocopier-unpowered = It has no power.
photocopier-needs-canvases = Put a painting and a blank canvas in first.
photocopier-nothing-to-copy = Both canvases are blank.
photocopier-needs-blank = One of the canvases needs to be blank and unsigned.
photocopier-size-mismatch = The blank canvas has to be the same size as the painting.

cmd-paintloadurl-busy = Another image import is still running.

tiles-canvas-floor = canvas floor
stack-canvas-tile = {$amount ->
    [1] canvas tile
    *[other] canvas tiles
}

ent-PhotocopierMachineCircuitboard = photocopier machine board
    .desc = A machine printed circuit board for a photocopier.

ent-CrayonRainbowLarge = plasmic rainbow crayon
    .desc = A colourful crayon. Now infinite! (almost)

ent-PaintCanvas = canvas
    .desc = A blank square canvas. Use a crayon, paint or a palette on it to start painting.

ent-PaintCanvasLandscape = landscape canvas
    .desc = A wide blank canvas, good for scenery.

ent-PaintCanvasSmall = small canvas
    .desc = A tiny blank canvas, perfect for doodles and portraits.

ent-Paintbrush = artist's palette
    .desc = A palette and brush to mix any colour you like.

ent-PaintCan = paint can
    .desc = A metal can for paint. Pour any coloured liquid in to paint with it.

ent-PaintCanRed = red paint
    .desc = A can of thick red paint.

ent-PaintCanYellow = yellow paint
    .desc = A can of thick yellow paint.

ent-PaintCanGreen = green paint
    .desc = A can of thick green paint.

ent-PaintCanBlue = blue paint
    .desc = A can of thick blue paint.

ent-PaintCanViolet = violet paint
    .desc = A can of thick violet paint.

ent-PaintCanBlack = black paint
    .desc = A can of thick black paint.

ent-PaintCanWhite = white paint
    .desc = A can of thick white paint.

ent-BasePaintingFrame = { "" }
    .desc = Put it on a finished painting to frame it.

ent-PaintingFrameWood = wooden painting frame

ent-PaintingFrameIron = iron painting frame

ent-PaintingFrameBamboo = bamboo painting frame

ent-PaintingFrameBones = bone painting frame

ent-PaintingFrameBronze = bronze painting frame

ent-PaintingFrameClown = clown painting frame

ent-PaintingFrameFrog = frog painting frame

ent-PaintingFrameSilver = silver painting frame

ent-PaintingFrameNecropolis = necropolis painting frame

ent-PaintingFrameGold = gold painting frame

ent-PaintingFrameDiamond = diamond painting frame

ent-PaintingFrameRainbow = rainbow painting frame

ent-PaintingFrameSupermatter = supermatter painting frame
    .desc = Put it on a finished painting to frame it. It hums faintly.

ent-PaintEasel = easel
    .desc = A wooden stand that holds a canvas while you paint on it.

ent-Photocopier = photocopier
    .desc = Copies a painting onto a blank canvas of the same size. Insert both, then switch it on.

ent-FloorTileItemCanvas = canvas floor
