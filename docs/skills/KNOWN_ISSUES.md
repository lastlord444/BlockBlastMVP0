# KNOWN ISSUES — Block Blast MVP0

## ✓ Çözülenmiş
- ~~NullReferenceException in App.cs OnEnable/OnDisable~~ → Fixed: Null guard eklendi (domain reload güvenliği)
- ~~Shape slot overflow (4-blok şekil 166px taşma)~~ → Fixed: Dinamik blockSize hesabı
- ~~Line clear juice spam (tile başına 10x SFX)~~ → Fixed: Per-line juice + HashSet dedupe
- ~~Particle fallback performans (per-spawn Texture2D)~~ → Fixed: Static sprite cache

## Açık (MVP0)
- Safe Area: Bottom tray 21:9 aspect'te kontrol edilmedi (ama kod düzgün, muhtemelen çalışıyor)
- Audio clip'ler yoksa → sessizce çalışıyor (hata yok)

## MVP1 için planlanan
- Particle prefab → daha profesyonel görünüm
- Object pool (20+ clear sonrasi GC spike kontrolü)
- Tween lib yerine DOTween (projede var) kullanma

## Notlar
- Domain reload sonrası App.cs null guard kritik — tüm event subscribe'ları null kontrolü olmalı
- UnityEditor-only kodlarda #pragma warning disable CS0618 gerekli (obsolete API'ler için)
