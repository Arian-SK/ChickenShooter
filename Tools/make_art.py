"""Generate original art and extract the selected CC0 sprites from Kenney's ZIP."""
from pathlib import Path
from zipfile import ZipFile
import random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

root = Path(__file__).resolve().parents[1]
out = root / "Assets/Cluck/Art"
out.mkdir(parents=True, exist_ok=True)
archive = root / "Kenney-source.zip"
if archive.exists():
    with ZipFile(archive) as pack:
        for source, target in {
            "PNG/playerShip1_blue.png": "ship.png",
            "PNG/Meteors/meteorBrown_big1.png": "asteroid.png",
            "PNG/Effects/shield3.png": "shield.png",
            "Bonus/kenvector_future.ttf": "display.ttf",
        }.items():
            (out / target).write_bytes(pack.read(source))


def make_sprite(name, draw, size=(128, 128)):
    img = Image.new("RGBA", (size[0] * 3, size[1] * 3))
    draw(ImageDraw.Draw(img))
    img.resize(size, Image.Resampling.LANCZOS).save(out / (name + ".png"))


def chicken(d, tier, damage=0):
    def ellipse(box, fill, edge=None, width=5):
        d.ellipse(tuple(int(v * 3) for v in box), fill, edge, width * 3)
    def poly(points, fill, edge="#152036", width=4):
        p = [(int(x * 3), int(y * 3)) for x, y in points]
        d.polygon(p, fill)
        if edge:
            d.line(p + p[:1], edge, width * 3, joint="curve")

    light, shade, trim = [("#fff6dc", "#aabed1", "#fe5d71"),
                          ("#d7e6f5", "#687d9d", "#fd964b"),
                          ("#d9bcff", "#7756bc", "#7efbd9")][tier]
    border = "#16233f"
    # Feet, comb, wings, then the face. Faces grow more armored in each sector.
    for x in (47, 80):
        poly([(x-5,89),(x+4,89),(x+9,108),(x,110),(x-8,105)], "#ff9b47")
    poly([(46,30),(43,17),(48,10),(56,16),(61,6),(69,8),(75,18),(83,13),(89,22),(81,32)], trim)
    for flip in [False, True]:
        def wing(p):
            return [(128-x if flip else x,y) for x,y in p]
        poly(wing([(40,43),(28,36),(7,45),(10,61),(16,72),(26,81),(38,78)]), shade)
        poly(wing([(32,47),(13,46),(21,59),(34,62)]), light, None)
        if tier:
            poly(wing([(12,54),(24,62),(15,69),(8,62)]), trim, None)
    ellipse((26,23,102,100), shade, border)
    ellipse((30,26,98,84), light)
    if tier:
        poly([(32,45),(43,28),(85,28),(96,45),(85,49),(64,40),(43,49)], "#4b5775" if tier == 1 else "#412666")
        poly([(57,30),(71,30),(64,38)], trim, None)
    for x in (47,81):
        ellipse((x-10,48,x+10,70), "#fdf9f2" if not tier else "#263247")
        ellipse((x-4,52,x+5,66), "#1d2a40" if not tier else trim)
        ellipse((x-2,52,x+1,55), "white")
    poly([(53,72),(64,65),(75,72),(64,81)], "#ffc34c", "#9b563c", 2)
    if tier == 2:
        poly([(33,76),(45,80),(40,88),(31,83)], trim, None)
        poly([(95,76),(83,80),(88,88),(97,83)], trim, None)

    def stroke(points, color, width):
        d.line([(x * 3, y * 3) for x, y in points], color, width * 3, joint="curve")

    if damage >= 1:
        # A scuff on the face and a nick on the wing.
        stroke([(37,73),(44,71),(49,73)], "#934d6a", 3)
        stroke([(13,55),(18,59),(22,57)], "#263449", 3)
    if damage >= 2:
        ellipse((77,49,94,69), "#824c70")
        stroke([(80,57),(85,54),(90,57)], "#f9dacd", 3)
        stroke([(82,32),(75,39),(79,43),(70,48)], "#4b324d", 3)
        stroke([(93,74),(98,69),(104,73)], "#4b324d", 3)
    if damage >= 3:
        ellipse((34,46,60,68), "#4b324d")
        stroke([(40,51),(54,62)], "#ffdfb8", 4)
        stroke([(53,51),(40,62)], "#ffdfb8", 4)
        poly([(79,21),(89,22),(83,33),(76,28)], "#4b324d", None)
        stroke([(57,83),(64,87),(72,83)], "#4b324d", 3)

for i in range(3):
    make_sprite("chicken" + str(i+1), lambda d, i=i: chicken(d, i))
    for damage, suffix in enumerate(("mild", "hurt", "bad"), 1):
        make_sprite(f"chicken{i+1}_{suffix}", lambda d, i=i, damage=damage: chicken(d, i, damage))


def egg(d):
    d.ellipse((32,8,112,151), "#fb8b60", "#4e263c", 8)
    d.ellipse((41,17,105,120), "#ffe0a6")
    d.ellipse((52,32,69,74), "#fff6db")
    d.line([(99,96),(74,106),(82,119),(58,128)], "#cf715f", 5)


def gift(d):
    d.rounded_rectangle((18,37,162,153), 18, "#18a78d", "#193948", 10)
    d.rounded_rectangle((12,24,168,65), 12, "#58f2bf", "#193948", 8)
    d.polygon([(78,32),(58,15),(75,3),(89,19),(104,3),(122,15),(100,32)], "#d9ff8c")
    d.rectangle((78,29,100,148), "#d9ff8c")
    d.polygon([(28,83),(65,77),(65,87),(28,100)], "#8ef6cf")


def laser(d):
    d.rounded_rectangle((13,5,59,163), 22, "#22d6ff44")
    d.rounded_rectangle((20,10,52,156), 16, "#29d8ff99")
    d.rounded_rectangle((27,13,45,150), 10, "#9df8ff")
    d.rounded_rectangle((31,16,41,142), 5, "#f4ffff")


def flare(d):
    d.polygon([(18,8),(65,8),(59,61),(42,154),(25,66)], "#3dbdf088")
    d.polygon([(25,10),(58,10),(49,71),(42,119),(34,74)], "#74edffe4")
    d.polygon([(31,11),(53,11),(42,77)], "#f3ffff")


make_sprite("egg", egg, (48,54))
make_sprite("gift", gift, (60,54))
make_sprite("laser", laser, (24,56))
make_sprite("flare", flare, (28,54))
make_sprite("spark", lambda d: d.ellipse((4,3,17,66), "#fff8dc"), (7,23))
make_sprite("ring", lambda d: d.ellipse((9,9,375,375), outline="#c9faff", width=7))


def damaged_asteroid(d, split):
    def polygon(points, color, outline=None):
        points = [(x * 3, y * 3) for x, y in points]
        d.polygon(points, color)
        if outline:
            d.line(points + points[:1], outline, 7, joint="curve")

    def crack(points, color, width):
        d.line([(x * 3, y * 3) for x, y in points], color, width * 3, joint="curve")

    rim = [(18,2),(67,2),(74,14),(83,12),(99,39),(85,72),(65,70),(29,82),(2,51)]
    if split:
        rim = [(18,3),(52,2),(57,16),(67,23),(76,19),(88,24),(96,39),(86,61),
               (75,59),(68,68),(60,67),(53,77),(29,81),(17,69),(24,59),(12,57),(2,50)]
    polygon(rim, "#826149", "#513d33")
    polygon([(28,14),(62,13),(75,24),(83,38),(76,55),(57,55),(40,64),(27,51),(18,49)], "#a57b5c")
    polygon([(37,26),(50,27),(49,36),(42,42),(33,36)], "#83614d")
    if split:
        polygon([(18,53),(35,56),(45,72),(31,77),(19,69),(26,60)], "#644739")
        crack([(54,4),(46,19),(58,29),(50,41),(61,53),(49,67),(54,75)], "#382b2a", 6)
        crack([(86,25),(73,30),(66,45),(52,42)], "#432e2a", 4)
        crack([(51,41),(39,44),(30,55),(23,54)], "#f6b46b", 3)
        crack([(56,50),(63,55),(56,63)], "#f4ac64", 3)
    else:
        crack([(72,9),(66,24),(72,34),(59,41),(55,55)], "#513731", 4)
        crack([(70,34),(57,32),(51,39)], "#e8a06a", 3)


make_sprite("asteroid_cracked", lambda d: damaged_asteroid(d, False), (101, 84))
make_sprite("asteroid_split", lambda d: damaged_asteroid(d, True), (101, 84))


def backdrop(stage):
    w,h = 1280,720
    x,y = np.meshgrid(np.arange(w),np.arange(h))
    rng = np.random.default_rng(127 + stage)
    randomizer = random.Random(727 + stage)
    deep, nebula, globe, center = [
        ((3,9,27),(13,74,120),(34,142,212),(1055,685,307)),
        ((13,8,25),(91,35,89),(229,139,89),(1126,450,235)),
        ((9,6,29),(78,40,141),(123,69,230),(1065,519,204))][stage-1]
    wave = 500 - .33*x + 47*np.sin(x/173) + stage*30
    mist = np.exp(-((y-wave)/165)**2)*(.48+.25*np.sin(x/170+y/58)+.13*np.cos(x/37-y/71))
    mist += .35*np.exp(-((x-1050)/480)**2-((y-330)/375)**2)
    frame = np.array(deep,dtype=float) + mist[:,:,None]*np.array(nebula)[None,None,:]
    frame += rng.normal(0,1.45,(h,w,1))
    sky = Image.fromarray(np.uint8(np.clip(frame,0,255)), "RGB")
    d = ImageDraw.Draw(sky)
    for i in range(540):
        px,py = randomizer.randrange(w),randomizer.randrange(h)
        b = randomizer.randrange(34,121)
        d.point((px,py),(b,b,min(255,b+22)))
        if i % 44 == 0:
            d.line((px-5,py,px+5,py), fill=(68,114,153))
            d.line((px,py-5,px,py+5), fill=(68,114,153))
            d.point((px,py),fill=(206,234,248))
    frame = np.array(sky).astype(float)
    cx,cy,r = center
    xx, yy = (x-cx)/r,(y-cy)/r
    dist = np.sqrt(xx**2+yy**2)
    z = np.sqrt(np.maximum(1-dist**2, 0))
    atmosphere = np.exp(-np.maximum(dist-1,0)*36)*np.exp(-np.maximum(1-dist,0)*5)
    frame += atmosphere[:,:,None]*np.array(globe)[None,None,:]*.25
    light = np.clip(-.48*xx-.60*yy+.55*z,.04,1)
    cloud = .51*np.sin(yy*24+xx*4+2*np.sin(xx*7)) + .27*np.cos(yy*63-xx*7) + .12*np.sin(xx*91+yy*59)
    if stage == 1:
        land = np.sin(xx*7+2*np.cos(yy*4))*np.sin(yy*8-xx*2) + .36*np.cos(xx*13-yy*7) + .23*np.sin(xx*29+yy*22)
        base = np.where((land>.17)[:,:,None],np.array([32,120,96]),np.array(globe))
        base = base+np.maximum(cloud-.18,0)[:,:,None]*101
    elif stage == 2:
        base = np.array(globe) + cloud[:,:,None]*np.array([49,42,38])
    else:
        base = np.array(globe) + (cloud*.31+.45*np.cos(xx*15+yy*7))[:,:,None]*np.array([48,44,41])
    globe_color = (base*light[:,:,None])*.69 + atmosphere[:,:,None]*np.array(globe)*.39
    frame = np.where((dist<=1)[:,:,None],globe_color,frame)
    if stage != 1:
        angle = -.24 if stage == 2 else -.44
        a,b = xx*np.cos(angle)+yy*np.sin(angle),-xx*np.sin(angle)+yy*np.cos(angle)
        ringd = np.sqrt(a*a+(b/.27)**2)
        ring = (ringd>1.13)&(ringd<1.72)&((dist>1)|(b>0))
        bands = (.16 + .14*np.sin(ringd*117)**2 + .12*np.sin(ringd*42)**2)*ring
        frame = frame*(1-bands[:,:,None]) + bands[:,:,None]*np.array(globe)
    # Keep the immediate playfield dark while leaving the planet rim visible.
    vignette = np.clip(1-.16*((x-w/2)/(w/2))**2-.14*((y-h/2)/(h/2))**2,.62,1)
    Image.fromarray(np.uint8(np.clip(frame*vignette[:,:,None],0,255))).save(out / f"sector{stage}.jpg", quality=91)


for stage in range(1,4):
    backdrop(stage)
