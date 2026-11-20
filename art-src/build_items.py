"""Draws the 84mm Rocket Case (our own art) and packs it as a game item mod (.zedmod = zip of
mod.json + sprite.png + preview.jpg + locale) into src/items/. The DLL embeds it and loads it at
start-up (src/HeliItems.cs), so installing Helicopter.dll is all it takes.
Run:  python art-src/build_items.py"""
import json, os, zipfile
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.join(HERE, "..")

# Never change the guid after release: saves refer to it.
CASE_GUID = "e6350b16-245b-4a66-84d3-b7fc36e98fbd"
CASE_RID = 881320  # requested runtime id; the game's loader remaps it if it clashes
CASE_NAME = "84mm Rocket Case"
CASE_DESC = ("The empty case of a fired 84mm rocket. The helicopter's rocket pods hand one back after every rocket. "
             "Take it to the reloader with gunpowder, an iron plate and a metal component to load a new 84mm HE Round. "
             "New cases are rolled from iron plate at the reloader.")

W, H = 256, 96

def draw_case():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cy = H // 2
    ol = (24, 26, 18, 255)
    # olive steel tube with a darker rear venturi and an open front
    d.rectangle([20, cy - 22, 236, cy + 22], fill=(96, 104, 66, 255), outline=ol, width=2)
    d.rectangle([24, cy - 18, 232, cy - 11], fill=(132, 140, 92, 255))   # shine
    d.rectangle([24, cy + 12, 232, cy + 18], fill=(70, 76, 48, 255))     # shade
    d.polygon([(4, cy - 16), (20, cy - 22), (20, cy + 22), (4, cy + 16)], fill=(60, 62, 56, 255), outline=ol)
    d.ellipse([228, cy - 20, 246, cy + 20], fill=(40, 40, 34, 255), outline=ol, width=2)  # open mouth
    for x in (60, 170):
        d.rectangle([x, cy - 22, x + 8, cy + 22], fill=(196, 160, 60, 255))  # brass bands
    d.rectangle([100, cy - 4, 140, cy + 4], fill=(214, 186, 40, 255))        # HE stripe
    return im

def preview_of(img, size, label):
    prev = Image.new("RGB", (512, 512), (34, 38, 30))
    pd = ImageDraw.Draw(prev)
    for y in range(0, 512, 32):
        pd.line([(0, y), (512, y)], fill=(40, 45, 36))
    big = img.resize(size, Image.LANCZOS)
    prev.paste(big, ((512 - size[0]) // 2, (512 - size[1]) // 2), big)
    pd.text((20, 470), label, fill=(220, 200, 120))
    return prev

def main():
    case = draw_case()
    art = os.path.join(REPO, "art-src", "case84")
    os.makedirs(art, exist_ok=True)
    case.save(os.path.join(art, "sprite.png"))
    preview_of(case, (448, 168), "84mm case").save(os.path.join(art, "preview.jpg"), quality=92)
    d = {
        "modType": "Ammo", "modGuid": CASE_GUID, "runtimeId": CASE_RID, "publishMode": 0, "targetNativeItemId": 0,
        "primaryLangCode": "en_US", "spritePath": "sprite.png", "ppu": 640,
        "spriteRegions": [{"slotName": "Main", "region": {"x": 2, "y": 24, "w": 248, "h": 48}}],
        "itemName": CASE_NAME, "itemName_EN": CASE_NAME, "itemDescription": CASE_DESC, "itemDescription_EN": CASE_DESC,
        "itemRarity": 0.6, "hiddenItem": False, "itemInventorySizeX": 3, "itemInventorySizeY": 1,
        "generateItemInRoom": False, "generateItemInField": False, "generateItemInCoastFloat": 0.0,
        "weight": 1.0, "itemPrice": 40.0, "stackNumber": 10,
        # Iron Plate (8) rolled into a new case at the reloader
        "craftRecipeItems": [{"itemId": 8, "itemNumber": 2.0}],
        "craftPlatform": 3, "craftAudioType": 0, "craftOutputNumber": 1,
        "repairRecipeItems": [{"itemId": 8, "itemNumber": 1.0}],
        "ammoNumber": 1, "ammoBaseDamage": 0.0,
    }
    locale = {"langCode": "en_US", "modGuid": CASE_GUID, "translatorNote": "", "itemName": CASE_NAME,
              "itemDescription": CASE_DESC, "extraFields": []}
    out = os.path.join(REPO, "src", "items")
    os.makedirs(out, exist_ok=True)
    path = os.path.join(out, CASE_GUID + ".zedmod")
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as z:
        z.writestr("mod.json", json.dumps(d, indent=4, ensure_ascii=False))
        z.write(os.path.join(art, "sprite.png"), "sprite.png")
        z.write(os.path.join(art, "preview.jpg"), "preview.jpg")
        z.writestr("locale/en_US.json", json.dumps(locale, indent=4, ensure_ascii=False))
    print("built", os.path.relpath(path, REPO), CASE_NAME)

if __name__ == "__main__":
    main()
