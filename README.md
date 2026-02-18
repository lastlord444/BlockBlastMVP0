# Block Blast MVP0 🎮

> Android hedefli **1010 / Block Blast** tarzı puzzle oyunu — 10x10 grid, 3 shape slot, drag-drop yerleştirme, row/col clear + combo sistemi.

> _Bu repo orijinalde Match3-SDK fork olarak başladı, ardından Block Blast MVP0'a evrildi. Yeni repo: [BlockBlastMVP0](https://github.com/lastlord444/BlockBlastMVP0)._

---

## ✅ MVP0 Feature Checklist

- [x] 10×10 board (checkerboard premium görünüm)
- [x] 3 shape slot + anında refill (Tetris-style parçalar)
- [x] Drag & drop + grid snap + placement validation
- [x] Row / column clear + skor sistemi + combo multiplier
- [x] Game Over: Mevcut parçalar hiçbir yere sığamazsa tetiklenir
- [x] Best Score: `PlayerPrefs` ile persistent
- [x] Ghost preview (gri=geçerli, kırmızı=geçersiz)
- [x] Safe area bottom tray fix (Android notch/home bar uyumlu)
- [x] **Juice v1** — SFX (place / invalid / clear / combo / gameover), haptic, camera shake
- [x] **Juice v2** — Line-based juice (1× SFX per line, HashSet dedupe), particle fallback cache
- [x] **Juice v2** — Place punch-scale (1.0→1.08→1.0, ~0.12s)
- [x] **Juice v2** — Preview shapes auto-center+fit (slot overflow fixed, 8px padding)

---

## 🚀 Nasıl Çalıştırılır

### Gereksinimler
- **Unity:** 6000.3.x LTS (6000.3.8f1 test edildi)
- **Platformlar:** Windows Editor, Android

### Kurulum
```bash
git clone https://github.com/lastlord444/BlockBlastMVP0.git
cd BlockBlastMVP0
# Unity Hub > Add > Proje klasörünü seç
# Unity 6000.3.x LTS ile aç (ilk açılışta ~2-3 dk package import)
```

### Oyunu Başlat
1. Unity Editor'de `Assets/Scenes/MainScene.unity` sahnesini aç
2. **Play** butonuna bas

> **Beklenen:** Console'da 0 error / 0 warning. Ekranın altında 3 shape slot görünür.

---

## 🎮 Kontroller

| Eylem | PC | Mobil |
|-------|----|-------|
| Parçayı sürükle | Sol tık tutup sürükle | Parmakla sürükle |
| Tahtaya bırak | Sol tıkı bırak | Parmağı kaldır |
| Ghost (önizleme) | Sürükleme sırasında otomatik | Otomatik |

---

## 📱 Android Build

```
Unity Editor → File → Build Settings
  → Platform: Android
  → ✅ IL2CPP Scripting Backend
  → ✅ ARM64
  → Build (debug) veya Build And Run
```

> Android Build Support'u Unity Hub > Installs > 6000.3.x > Modüller'den kurun.

---

## 📸 Proof

| Ekran | Açıklama |
|-------|----------|
| [`gameplay_juice_v2.png`](Assets/Screenshots/ProofPack_gameplay_juice_v2.png) | Line clear efektleri + skor artışı |
| [`console_clean.png`](Assets/Screenshots/console_clean.png) | Console: 0 error / 0 warning |

---

## 🛠️ Teknoloji Stack

| Kategori | Teknoloji |
|----------|-----------|
| Engine | Unity 6000.3.x LTS |
| Grid / Render | Match3-SDK (LibraStack) — renderer'ı koruduk, game logic yeniden yazıldı |
| Juice | `GameJuiceManager` + `LineClearSequencer` (coroutine-based) |
| UI | TextMeshPro + SafeAreaFitter |
| Build | IL2CPP + ARM64 |
| Persisted State | PlayerPrefs (best score)

---

## 📁 Önemli Script'ler

```
Assets/Scripts/Common/
├── GameModes/
│   └── BlockBlastGameManager.cs   ← Ana oyun döngüsü
├── Juice/
│   ├── GameJuiceManager.cs        ← SFX / haptic / shake yönetimi
│   └── LineClearSequencer.cs      ← Line clear VFX (flash + particle)
├── UI/
│   ├── ShapeSlot.cs               ← Auto-center+fit (8px padding)
│   ├── SafeAreaFitter.cs          ← Android safe area uyumu
│   └── DevOnlyVisibility.cs       ← Debug UI → release'de gizle
├── BlockBlastInputManager.cs      ← Drag / hover / drop events
├── BoardConfig.cs                 ← Grid yapılandırması (10×10, tile size)
└── Shapes/
    ├── ShapeData.cs
    └── ShapeGenerator.cs
```

---

## 🗺️ Roadmap

### MVP1 (Sonraki)
- [ ] Progressive difficulty (puanlar zorlaştıkça)
- [ ] Daily quests / missions
- [ ] Undo/Redo (son hamleyi geri al)
- [ ] Hint sistemi (olası yer gösterimi)

### MVP2
- [ ] Ads entegrasyonu (Admob banner + interstitial)
- [ ] Firebase Analytics (first_open, line_clear, game_over)
- [ ] Tema sistemi (renk paketi)
- [ ] Leaderboard (Play Games veya custom)

---

## 🔗 Bağlantılar

- **Repo (Yeni):** https://github.com/lastlord444/BlockBlastMVP0
- **Branch:** main (only branch)
- **Eski Repo (fork):** https://github.com/lastlord444/idlgames
- **Match3-SDK (base):** https://github.com/LibraStack/Match3-SDK

---

**Son Güncelleme:** 2026-02-18  
**Geliştirici:** @lastlord444  
**Durum:** 🟢 MVP0 READY FOR RELEASE
