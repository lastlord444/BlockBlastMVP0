# MENTOR (REHBER)

Proje geliştirme prensipleri ve kuralları. Tek doğruluk kaynağı (SSOT): `/docs/skills/` klasörüdür.

## Kalite Kapıları (Quality Gates)
- **Console Error = 0**: Konsolda 1 tane bile kırmızı hata (Error) varsa değişiklik "tamamlandı" sayılamaz. Commit atılamaz.
- **Proof Pack Zorunluluğu**: Her görev sonunda `Assets/Screenshots/ProofPack/` altında güncel PNG kanıtlar (GameView, Console, Inspector) olmalıdır. TXT kanıt kabul edilmez.
- **Düşük Risk**: Küçük ve izole değişiklikler yapın. Büyük refactor yasaktır. Çalışan kodu bozmayın.

## Regresyon Test Protokolü
Play Mode'da şu adımlar doğrulanmalıdır:
1. **Drop**: Blok başarıyla bırakılabiliyor.
2. **Invalid Drop**: Geçersiz yere bırakılan blok havuza geri dönüyor.
3. **Clear**: Satır/sütun dolunca siliniyor ve skor artıyor.
4. **Console**: Oyun süresince Error 0, Warning 0 (veya bilinen listesinde) olmalı.

## Debug & Loglama
- Debug logları sadece `DEVELOPMENT_BUILD` veya `UNITY_EDITOR` makroları içinde olmalıdır.
- Release build'lerde log spam'i olmamalıdır.

## Mimari Kurallar
- **BoardConfig**: Grid boyutları (Rows/Cols) için tek doğruluk kaynağıdır. Hardcode değer kullanmayın.
- **Block Blast Core**: Mevcut çalışan core loop korunmalıdır.
