# REGRESSION GUARD (GERİLEME KORUMASI)

Kod değişikliği yapmadan veya commit atmadan önce bu listeyi kontrol et.

## Pre-Commit Kontrol Listesi
- [ ] **Console Error = 0**: Hiç hata yok mu?
- [ ] **Console Warning = 0**: (İstisnalar KNOWN_ISSUES'da listeli mi?)
- [ ] **Play Mode: Drop**: 1 blok başarıyla bırakıldı mı?
- [ ] **Play Mode: Invalid Drop**: Yanlış yere bırakılan blok resetlendi mi?
- [ ] **Play Mode: Clear**: Satır silinince skor arttı mı?
- [ ] **Android RC Build**: Build alınıp cihazda açılıyor mu? (Development Build OFF)
- [ ] **UI SafeArea**: Çentikli ekranlarda UI taşması var mı? (Device Simulator + 1 gerçek cihaz)
- [ ] **Proof Pack**: PNG kanıtlar güncel mi?

## Rollback Planı
Eğer build patlarsa:
1. Son çalışan commit'e dön (`git checkout`).
2. `Library` klasörünü temizle.
3. Sorunu raporla.
