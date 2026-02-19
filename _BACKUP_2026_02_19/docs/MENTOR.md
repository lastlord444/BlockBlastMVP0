# MENTOR.md - Geliştirme Prensipleri

## Karar İlkeleri
1. **MVP Odaklılık:** Sadece o anki faz için gerekli olanı kodla. "Belki lazım olur" diye özellik ekleme.
2. **Kodu Basit Tut:** Karmaşık mimariler yerine okunabilir ve düz kod yaz.
3. **Mevcut Yapıyı Koru:** Çalışan bir Match3 altyapısı var, bunu bozmadan dönüştür.
4. **Hata Toleransı:** Kullanıcı (oyuncu) hata yapabilir, sistem çökmemeli.
5. **Unity Standartları:** Unity'nin kendi API'larını (Grid, Tilemap, vb.) kullan, tekerleği yeniden icat etme.

## YAPILMAYACAKLAR (Anti-Patterns)
- ❌ **Over-engineering:** Tek bir işlev için 5 farklı sınıf/arayüz oluşturma.
- ❌ **Magic Numbers:** Kod içine `10`, `0.5f` gibi sayıları gömme, `BoardConfig` veya `const` kullan.
- ❌ **Deep Inheritance:** 3 seviyeden fazla kalıtım kullanma.
- ❌ **Global State:** Singleton kullanımını minimize et, dependency injection (veya inspector reference) tercih et.
- ❌ **Eski Kod Bırakma:** Kullanılmayan Match3 kodlarını ("swap", "match controller") temizle veya `#if false` ile kapat, yorum satırı yığını bırakma.
