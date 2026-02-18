# REGRESSION_GUARD.md - Kalite Kontrol

## Commit Öncesi Kontrol Listesi

### 1. Konsol Temizliği
- [ ] Unity Console'da **Kırmızı Hata (Error)** YOK.
- [ ] Unity Console'da **Sarı Uyarı (Warning)** YOK (veya bilinen ve kabul edilenler dışında yok).

### 2. Play Mode Testi
- [ ] Oyun başlatıldığında Board 10x10 olarak geliyor mu?
- [ ] UI elementleri (Shape Slots) görünüyor ve etkileşime girilebilir mi?
- [ ] Bir şekil sürüklenip bırakılabiliyor mu?
- [ ] **Kritik:** Match3 swap özelliği *çalışmıyor* mu? (Eski mekanik kapalı olmalı).

### 3. Build Testi (Haftalık)
- [ ] Android APK build alınabiliyor mu?
- [ ] Telefonda (gerçek cihazda) FPS 30+ mı?

### 4. Kod Standartları
- [ ] Gereksiz `Debug.Log`'lar temizlendi mi?
- [ ] `TODO` blokları açıldı mı? (Eksik kalan yerler için).
