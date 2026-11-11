ent-MobFillBot = fillbot
    .desc = Picks up compatible loose materials and loads the machine linked with a multitool. When linked to a disposal unit, it collects loose trash.
ent-MobPlantbot = plantbot
    .desc = A botanist's best friend! It waters and weeds nearby trays automatically.

petting-success-plantbot = You pet {THE($target)} on {POSS-ADJ($target)} muddy metal head.
petting-success-fillbot = You pet {THE($target)} on {POSS-ADJ($target)} diligent metal head.
petting-failure-plantbot = You reach out to pet {THE($target)}, but {POSS-ADJ($target)} clippers nearly snip your fingers off!
petting-failure-fillbot = You reach out to pet {THE($target)}, but {SUBJECT($target)} pushes your hand away!

signal-port-name-fillitems = Fillbot Items
signal-port-description-fillitems = Link to the machine or disposal unit this bot should load.
signal-port-name-fill-any-item = Fillbot Target
signal-port-description-fill-any-item = Receives suitable materials or trash carried by a linked fillbot.
