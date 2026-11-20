"""Recolour BDArmory's green HUD textures to BDArmory Lite's orange (BDAStyle.Title, #F0A33C).

Standard library only: a small PNG decoder (8-bit grey, grey+alpha, RGB, RGBA and palette images; all filter types;
no interlacing) and encoder (8-bit RGBA, filter 0).

Each pixel whose hue is green keeps its alpha and brightness (HSV value) and has its hue swapped for the orange's;
its saturation is scaled by the orange's saturation, so pure green becomes the orange and paler greens paler orange.
White, grey, black and non-green pixels are left alone.

Usage as a script: python3 -I tools/recolor.py in.png out.png
"""
import colorsys, struct, sys, zlib

ORANGE = (0xF0, 0xA3, 0x3C)
ORANGE_H, ORANGE_S, _ = colorsys.rgb_to_hsv(*(c / 255 for c in ORANGE))
GREEN_HUE = (70 / 360, 170 / 360)   # Hues (HSV, 0-1) treated as green.
MIN_SAT = 0.15                      # Below this a pixel is grey/white and left alone.

PNG_SIG = b'\x89PNG\r\n\x1a\n'
CHANNELS = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}


def _paeth(a, b, c):
    p = a + b - c
    pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
    if pa <= pb and pa <= pc:
        return a
    return b if pb <= pc else c


def decode(data):
    """Return (width, height, rows) with rows a list of bytearrays of RGBA pixels."""
    if not data.startswith(PNG_SIG):
        raise ValueError('not a PNG')
    pos, idat, plte, trns = 8, [], None, None
    while pos < len(data):
        length, ctype = struct.unpack('>I4s', data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if ctype == b'IHDR':
            w, h, depth, colour, _, _, interlace = struct.unpack('>IIBBBBB', body)
        elif ctype == b'PLTE':
            plte = body
        elif ctype == b'tRNS':
            trns = body
        elif ctype == b'IDAT':
            idat.append(body)
        elif ctype == b'IEND':
            break
    if depth != 8 or colour not in CHANNELS or interlace:
        raise ValueError(f'unsupported PNG (bit depth {depth}, colour type {colour}, interlace {interlace})')
    bpp = CHANNELS[colour]
    stride = w * bpp
    raw = zlib.decompress(b''.join(idat))
    rows, prev = [], bytearray(stride)
    for y in range(h):
        off = y * (stride + 1)
        ftype, line = raw[off], bytearray(raw[off + 1:off + 1 + stride])
        if ftype == 1:
            for i in range(bpp, stride):
                line[i] = (line[i] + line[i - bpp]) & 255
        elif ftype == 2:
            for i in range(stride):
                line[i] = (line[i] + prev[i]) & 255
        elif ftype == 3:
            for i in range(stride):
                line[i] = (line[i] + (((line[i - bpp] if i >= bpp else 0) + prev[i]) >> 1)) & 255
        elif ftype == 4:
            for i in range(stride):
                a = line[i - bpp] if i >= bpp else 0
                c = prev[i - bpp] if i >= bpp else 0
                line[i] = (line[i] + _paeth(a, prev[i], c)) & 255
        elif ftype != 0:
            raise ValueError(f'bad filter type {ftype}')
        prev = line
        rgba = bytearray(w * 4)
        for x in range(w):
            if colour == 6:
                rgba[x * 4:x * 4 + 4] = line[x * 4:x * 4 + 4]
            elif colour == 2:
                r, g, b = line[x * 3:x * 3 + 3]
                a = 0 if trns and len(trns) >= 6 and (r, g, b) == struct.unpack('>HHH', trns[:6]) else 255
                rgba[x * 4:x * 4 + 4] = bytes((r, g, b, a))
            elif colour == 0:
                v = line[x]
                a = 0 if trns and len(trns) >= 2 and v == struct.unpack('>H', trns[:2])[0] else 255
                rgba[x * 4:x * 4 + 4] = bytes((v, v, v, a))
            elif colour == 4:
                v, a = line[x * 2:x * 2 + 2]
                rgba[x * 4:x * 4 + 4] = bytes((v, v, v, a))
            else:  # palette
                i = line[x]
                r, g, b = plte[i * 3:i * 3 + 3]
                a = trns[i] if trns and i < len(trns) else 255
                rgba[x * 4:x * 4 + 4] = bytes((r, g, b, a))
        rows.append(rgba)
    return w, h, rows


def encode(w, h, rows):
    def chunk(ctype, body):
        return struct.pack('>I', len(body)) + ctype + body + struct.pack('>I', zlib.crc32(ctype + body) & 0xffffffff)
    raw = b''.join(b'\x00' + bytes(r) for r in rows)
    return (PNG_SIG + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


def recolor_pixel(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    if s < MIN_SAT or not GREEN_HUE[0] <= h <= GREEN_HUE[1]:
        return r, g, b
    nr, ng, nb = colorsys.hsv_to_rgb(ORANGE_H, s * ORANGE_S, v)
    return round(nr * 255), round(ng * 255), round(nb * 255)


def recolor(data):
    """Recolour PNG bytes; returns (new PNG bytes, number of pixels changed)."""
    w, h, rows = decode(data)
    cache, changed = {}, 0
    for row in rows:
        for x in range(0, w * 4, 4):
            if row[x + 3] == 0:
                continue
            rgb = (row[x], row[x + 1], row[x + 2])
            new = cache.get(rgb)
            if new is None:
                new = cache[rgb] = recolor_pixel(*rgb)
            if new != rgb:
                row[x:x + 3] = bytes(new)
                changed += 1
    return encode(w, h, rows), changed


def recolor_file(src, dst=None):
    out, changed = recolor(open(src, 'rb').read())
    open(dst or src, 'wb').write(out)
    return changed


if __name__ == '__main__':
    print(recolor_file(sys.argv[1], sys.argv[2]), 'pixels recoloured')
