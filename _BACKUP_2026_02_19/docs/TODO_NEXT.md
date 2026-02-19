# TODO - Block Blast Klonu Geliştirme Adımları

## Proje Hedefi
Match3-SDK altyapısını kullanarak **Block Blast** tarzı bir puzzle oyunu yapmak.

## Faz 1: Skeleton Hazırlık (✅ TAMAMLANDI)
- [x] Match3-SDK reposunu klonla
- [x] Unity 6000.3.x LTS ile proje aç
- [x] Git repo ve branch yapısını kur
- [x] Mentor dokümanlarını hazırla

## Faz 2: Core Mekanik (Block Blast'a Dönüşüm)

### 2.1 Board (Oyun Tahtası)
- [ ] Match3 board'unu 8x8 sabit grid'e dönüştür
- [ ] Tile (hücre) prefab'ı hazırla
- [ ] Grid koordinat sistemi kur (0-7 x 0-7)
- [ ] Boş/dolu hücre mantığı
- [ ] **Risk:** Match3-SDK'nın default match3 mantığını devre dışı bırakmalıyız

### 2.2 Pieces (Bloklar)
- [ ] Tetris benzeri piece şekilleri (L, I, T, Square, vs.)
- [ ] Piece rotation sistemi (90°, 180°, 270°)
- [ ] Drag & drop mekanik
- [ ] Yerleştirilebilir alan kontrolü (collision check)
- [ ] Piece renk/tema sistemi

#### Block Şekilleri (İlk Set)
1. **I-Block:** 1x3, 1x4
2. **Square:** 2x2
3. **L-Block:** 3 varyant
4. **T-Block:** 2 varyant
5. **Z-Block:** 2 varyant

### 2.3 Line Clear System
- [ ] Yatay satır kontrolü (tüm satır doluysa clear)
- [ ] Dikey kolon kontrolü (tüm kolon doluysa clear)
- [ ] Combo sistemi (aynı anda birden fazla line)
- [ ] Clear animasyonu (DOTween kullan)
- [ ] Particle effect (patlama)

### 2.4 Scoring (Skor)
- [ ] Her block yerleştirmede +X puan
- [ ] Line clear başına +Y puan
- [ ] Combo bonusu (2x, 3x, 4x multiplier)
- [ ] High score kaydı (PlayerPrefs)
- [ ] Score UI (TextMeshPro)

### 2.5 Next Pieces (Sonraki Bloklar)
- [ ] 3 adet "next piece" preview
- [ ] Random piece generation algoritması
- [ ] Piece pool sistemi (adil dağılım)
- [ ] UI yerleşimi (ekranın alt/yan kısmı)

### 2.6 Game Over Kontrolü
- [ ] Verilen 3 piece'den hiçbiri yerleştirilemiyorsa game over
- [ ] Uyarı sistemi (kırmızı highlight)
- [ ] Restart butonu
- [ ] "Son oyun" istatistikleri popup

## Faz 3: Polish (Parlatma)

### 3.1 UI/UX
- [ ] Ana menü
- [ ] Pause menü
- [ ] Settings (ses, müzik, vibration)
- [ ] Tutorial (ilk 3 hamle)

### 3.2 Audio
- [ ] Piece yerleştirme sesi
- [ ] Line clear sesi
- [ ] Combo sesi (farklı tonlar)
- [ ] Background music (loop)
- [ ] Vibration feedback (Android)

### 3.3 Visual Effects
- [ ] Grid ızgara çizgileri
- [ ] Block gölgesi (drop önizleme)
- [ ] Parlama efekti (line clear)
- [ ] Screen shake (combo)

### 3.4 Optimizasyon
- [ ] Object pooling (block instantiate yerine)
- [ ] Texture atlasing (single draw call)
- [ ] Particle limitleme
- [ ] Profiler ile FPS kontrolü (60 FPS hedef)

## Faz 4: Android Build & Test

### 4.1 Build Ayarları
- [ ] Minimum API Level: 24 (Android 7.0)
- [ ] Target API Level: 34 (Android 14)
- [ ] IL2CPP + ARM64 backend
- [ ] Stripping Level: Medium
- [ ] .aab (Android App Bundle) formatı

### 4.2 Test
- [ ] Fiziksel cihazda test (minimum 3 farklı cihaz)
- [ ] Landscape/portrait orientation
- [ ] Farklı ekran çözünürlükleri (16:9, 18:9, 19.5:9)
- [ ] Performance profiling (battery, CPU, memory)

## Faz 5: Store Hazırlık

### 5.1 Store Assets
- [ ] Icon (512x512 PNG)
- [ ] Screenshots (minimum 4 adet)
- [ ] Feature graphic (1024x500)
- [ ] Promo video (opsiyonel)

### 5.2 Metadata
- [ ] Oyun adı
- [ ] Kısa açıklama (80 karakter)
- [ ] Uzun açıklama
- [ ] Anahtar kelimeler

---

## Öncelik Sırası (Sprint Planning)

### Sprint 1 (1 Hafta)
1. Board (8x8 grid)
2. Basit block yerleştirme (sadece 2x2 square)
3. Manuel line clear testi

### Sprint 2 (1 Hafta)
1. 5-6 farklı block şekli
2. Otomatik line clear sistemi
3. Basit scoring

### Sprint 3 (1 Hafta)
1. Next pieces (3 adet)
2. Game over kontrolü
3. Restart mekanik

### Sprint 4 (1 Hafta)
1. UI polish
2. Sound effects
3. İlk Android build

---

**Not:** Her sprint sonunda mutlaka **Android build test** yap!  
**Kritik:** Match3-SDK'nın Match-3 mantığını erken devre dışı bırak, yoksa ileride büyük refactor gerekebilir.
