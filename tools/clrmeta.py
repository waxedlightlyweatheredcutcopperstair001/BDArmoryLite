"""Minimal ECMA-335 metadata reader (stdlib only). Dumps types/methods/fields/typerefs/memberrefs."""
import struct, sys, json

data = open(sys.argv[1], 'rb').read()
pe = struct.unpack_from('<I', data, 0x3c)[0]
nsec = struct.unpack_from('<H', data, pe + 6)[0]
optsz = struct.unpack_from('<H', data, pe + 20)[0]
opt = pe + 24
magic = struct.unpack_from('<H', data, opt)[0]
ddir = opt + (96 if magic == 0x10b else 112)
secs = []
s0 = opt + optsz
for i in range(nsec):
    o = s0 + 40 * i
    vsz, va, rsz, rptr = struct.unpack_from('<IIII', data, o + 8)
    secs.append((va, max(vsz, rsz), rptr))

def rva(r):
    for va, sz, p in secs:
        if va <= r < va + sz:
            return r - va + p
    raise ValueError(hex(r))

cli_rva = struct.unpack_from('<I', data, ddir + 14 * 8)[0]
cli = rva(cli_rva)
md_rva, md_sz = struct.unpack_from('<II', data, cli + 8)
md = rva(md_rva)
vlen = struct.unpack_from('<I', data, md + 12)[0]
p = md + 16 + vlen
nstreams = struct.unpack_from('<H', data, p + 2)[0]
p += 4
streams = {}
for _ in range(nstreams):
    off, sz = struct.unpack_from('<II', data, p)
    p += 8
    e = data.index(b'\0', p)
    name = data[p:e].decode()
    p = (e + 4) & ~3
    streams[name] = (md + off, sz)

strs_off = streams['#Strings'][0]
def S(i):
    e = data.index(b'\0', strs_off + i)
    return data[strs_off + i:e].decode('utf8', 'replace')

t = streams['#~'][0]
heapsizes = data[t + 6]
valid = struct.unpack_from('<Q', data, t + 8)[0]
p = t + 24
rows = {}
for i in range(64):
    if valid >> i & 1:
        rows[i] = struct.unpack_from('<I', data, p)[0]; p += 4
SI = 4 if heapsizes & 1 else 2
GI = 4 if heapsizes & 2 else 2
BI = 4 if heapsizes & 4 else 2
def idx(tab): return 4 if rows.get(tab, 0) > 0xffff else 2
def coded(tabs, bits): return 4 if max(rows.get(x, 0) for x in tabs) >= (1 << (16 - bits)) else 2
TDOR = coded([2, 1, 0x1b], 2)
RS = coded([0, 0x1a, 0x23, 1], 2)
MRP = coded([2, 1, 0x1a, 6, 0x1b], 3)
HC = coded([4, 8, 0x17], 2)
HCA = coded([6,4,1,2,8,9,10,0,0xe,0x17,0x14,0x11,0x1a,0x1b,0x20,0x23,0x26,0x27,0x28,0x2a,0x2c,0x2b], 5)
HFM = coded([4, 8], 1)
HDS = coded([2, 6, 0x20], 2)
CAT = coded([6, 10], 3)
MDO = coded([6, 10], 1)
MF = coded([4, 6], 1)
IMPL = coded([0x26, 0x23, 0x27], 2)
TOMD = coded([2, 6], 1)
H = 4 if False else None
schemas = {
 0x00: [2, SI, GI, GI, GI], 0x01: [RS, SI, SI], 0x02: [4, SI, SI, TDOR, idx(4), idx(6)],
 0x03: [idx(4)], 0x04: [2, SI, BI], 0x05: [idx(6)], 0x06: [4, 2, 2, SI, BI, idx(8)],
 0x07: [idx(8)], 0x08: [2, 2, SI], 0x09: [idx(2), TDOR], 0x0a: [MRP, SI, BI], 0x0b: [2, HC, BI],
 0x0c: [HCA, CAT, BI], 0x0d: [HFM, BI], 0x0e: [2, HDS, BI], 0x0f: [2, 4, idx(2)], 0x10: [4, idx(4)],
 0x11: [BI], 0x12: [idx(2), idx(0x14)], 0x13: [idx(0x14)], 0x14: [2, SI, TDOR], 0x15: [idx(2), idx(0x17)],
 0x16: [idx(0x17)], 0x17: [2, SI, BI], 0x18: [2, idx(6), coded([0x14, 0x17], 1)], 0x19: [idx(2), MDO, MDO],
 0x1a: [SI], 0x1b: [BI], 0x1c: [2, MF, SI, idx(0x1a)], 0x1d: [4, idx(4)], 0x1e: [4, 4], 0x1f: [4],
 0x20: [4, 2, 2, 2, 2, 4, BI, SI, SI], 0x21: [4], 0x22: [4, 4, 4], 0x23: [2, 2, 2, 2, 4, BI, SI, SI, BI],
 0x24: [4, idx(0x23)], 0x25: [4, 4, 4, idx(0x23)], 0x26: [4, SI, BI], 0x27: [4, SI, SI, IMPL],
 0x28: [4, 4, SI, IMPL], 0x29: [idx(2), idx(2)], 0x2a: [2, 2, TOMD, SI], 0x2b: [MDO, BI], 0x2c: [idx(0x2a), TDOR],
}
tables = {}
for i in sorted(rows):
    sch = schemas[i]
    lst = []
    for _ in range(rows[i]):
        r = []
        for w in sch:
            r.append(struct.unpack_from('<H' if w == 2 else '<I', data, p)[0]); p += w
        lst.append(r)
    tables[i] = lst

typerefs = tables.get(1, [])
def trname(i):
    r = typerefs[i - 1]
    return (S(r[2]) + '.' if r[2] else '') + S(r[1])
typedefs = tables[2]
def tdname(i):
    r = typedefs[i - 1]
    return (S(r[2]) + '.' if r[2] else '') + S(r[1])
def tdor(v):
    tag, i = v & 3, v >> 2
    if i == 0: return ''
    return tdname(i) if tag == 0 else trname(i) if tag == 1 else 'TypeSpec#%d' % i

mode = sys.argv[2]
if mode == 'types':
    meth, fld = tables[6], tables[4]
    for n, r in enumerate(typedefs, 1):
        fe = typedefs[n][4] if n < len(typedefs) else len(fld) + 1
        me = typedefs[n][5] if n < len(typedefs) else len(meth) + 1
        print('TYPE %s : %s' % (tdname(n), tdor(r[3])))
        fs = [S(fld[i - 1][1]) for i in range(r[4], fe)]
        ms = [S(meth[i - 1][3]) for i in range(r[5], me)]
        if fs: print('  F ' + ', '.join(fs))
        if ms: print('  M ' + ', '.join(ms))
elif mode == 'nested':
    for r in tables.get(0x29, []):
        print(tdname(r[0]), '<-', tdname(r[1]))
elif mode == 'refs':
    mrs = tables.get(0x0a, [])
    for r in mrs:
        tag, i = r[0] & 7, r[0] >> 3
        parent = trname(i) if tag == 1 else ('TypeDef:' + tdname(i)) if tag == 0 else 'tag%d#%d' % (tag, i)
        print(parent + '::' + S(r[1]))
elif mode == 'typerefs':
    for i in range(1, len(typerefs) + 1):
        print(trname(i))
elif mode == 'asmrefs':
    for r in tables.get(0x23, []):
        print(S(r[6]), '%d.%d.%d.%d' % tuple(r[:4]))
elif mode == 'modrefs':
    for r in tables.get(0x1a, []): print(S(r[0]))
    for r in tables.get(0x1c, []): print('PINVOKE', S(r[2]))
