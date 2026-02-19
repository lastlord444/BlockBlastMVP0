# MIGRATION — Remove Fork Badge

## Why
Repo GitHub'da "forked from Match3-SDK" badge gösteriyor. MVP0 Block Blast'ın özgün proje olduğu için fork'tan çıkmak gerekli.

## Solution
Yeni public repo aç: **`lastlord444/BlockBlastMVP0`**

### Adım 1: GitHub'da yeni repo aç
1. github.com/new
2. Repository name: `BlockBlastMVP0`
3. Public
4. **Skip** "Initialize with README" (mevcut proje var)
5. Create

### Adım 2: Local'de push
```bash
cd c:\Users\musab\OneDrive\Desktop\idlgames
git remote add origin2 https://github.com/lastlord444/BlockBlastMVP0.git
git branch -M main  # Rename master → main
git push -u origin2 main
```

### Adım 3: Verify
- https://github.com/lastlord444/BlockBlastMVP0 açılmalı
- Fork badge kalmayacak ✓
- Main branch görünür

### Alternatif (gh CLI varsa — tek command)
```bash
gh repo create lastlord444/BlockBlastMVP0 --public --source . --remote origin2
git push origin2 main
```

## Eski Repo
`https://github.com/lastlord444/idlgames` → Match3-SDK fork kalır, ama blok blast değişiklikleri new repo'da.

## Links
- **Old (Fork)**: https://github.com/lastlord444/idlgames
- **New (MVP0)**: https://github.com/lastlord444/BlockBlastMVP0
