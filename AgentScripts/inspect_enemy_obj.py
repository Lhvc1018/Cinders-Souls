from pathlib import Path

files = [
    Path(r'C:\Users\Hp\Downloads\hellhound 3d model\hellhound_3d_model.obj'),
    Path(r'C:\Users\Hp\Downloads\horror inquisitor 3d model\horror_inquisitor_3d_model.obj'),
]
for path in files:
    low = [float('inf')] * 3
    high = [float('-inf')] * 3
    count = 0
    groups = []
    with path.open(errors='ignore') as source:
        for line in source:
            if line.startswith('v '):
                values = list(map(float, line.split()[1:4]))
                low = [min(a, b) for a, b in zip(low, values)]
                high = [max(a, b) for a, b in zip(high, values)]
                count += 1
            elif line.startswith(('o ', 'g ')):
                groups.append(line.strip())
    print(path.name, count, low, high, groups[:10])
