# MENTOR CHECKLIST - Unity Match3 Skeleton Projesi

## Her Değişiklik Öncesi Kontrol Listesi

### 1. Unity Sürüm Kontrolü
- [ ] Unity Editor sürümü: **6000.3.8f1 (Unity 6 LTS)** olmalı
- [ ] ProjectSettings/ProjectVersion.txt dosyasını kontrol et
- [ ] Package Manager'da uyumsuz paket var mı kontrol et

### 2. PlayMode Test
- [ ] Her kod değişikliğinden sonra Play Mode'da test et
- [ ] Scene'i kaydet ve Play'e bas
- [ ] En az 10 saniye hatasız çalıştığını doğrula

### 3. Console Temizliği
- [ ] Console'da **0 error, 0 warning** hedefi
- [ ] Varsa sadece info log'ları kabul edilebilir
- [ ] Stack trace'leri oku ve anla

### 4. Android Build Denemesi
- [ ] Build Settings > Android platform seçili mi?
- [ ] Minimum API Level: **24 (Android 7.0)** veya üzeri
- [ ] IL2CPP backend tercih edilmeli (ARM64 zorunlu)
- [ ] Test build alınabilir durumda mı? (Her hafta en az 1 kez)

## Her PR/Commit Öncesi

### 1. Değişen Dosya Listesi
```bash
git status
git diff --name-only
```
- Sadece gerekli dosyalar mı değişti?
- Library/, Temp/, UserSettings/ gibi klasörler commit'e dahil değil mi?

### 2. Kısa Risk Notu
Her commit message'ında şunları belirt:
- **Ne değişti?** (örn: "Grid boyutu 8x8'e çıkarıldı")
- **Risk nedir?** (örn: "Performans düşebilir, profiling gerekli")
- **Test edildi mi?** (örn: "✓ Play mode test edildi")

### 3. .gitignore Kontrolü
Şu dosyalar/klasörler asla commit edilmemeli:
- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `*.csproj`, `*.sln` (Package Manager tarafından generate ediliyor)

## Acil Durum Prosedürü

### Unity Çöktüyse
1. Temp/ klasörünü sil
2. Library/ klasörünü sil (dikkatli, reimport uzun sürebilir)
3. Unity'yi yeniden aç
4. Reimport bitene kadar bekle

### Git Sorunu Varsa
1. Branch'i yedekle: `git branch backup-$(date +%s)`
2. Clean state'e dön: `git reset --hard HEAD`
3. Gerekirse stash kullan: `git stash`

### Build Hatası Varsa
1. Build klasörünü tamamen sil
2. PlayerSettings'i sıfırla (ProjectSettings/ProjectSettings.asset)
3. Gradle cache temizle (Android için): `~/.gradle/caches`

---

**Son Güncelleme:** 2026-02-16  
**Unity Sürümü:** 6000.3.8f1  
**Platform:** Windows 11, Android (hedef)
