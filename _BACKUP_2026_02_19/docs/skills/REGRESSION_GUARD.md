# REGRESSION GUARD — Pre-Commit Checklist

Her commit öncesi:

1. **Console Kontrolü**
   - 0 ERROR zorunlu
   - 0 WARNING hedef (AMA: olabildiğince az)

2. **Play Mode Test**
   - 1 valid drop → block snaps + color değişir
   - 1 line clear → flash + particle + juice + skor artışı
   - 1 invalid drop → shake + invalid ses
   - Slot replenish çalışır
   - Game over check tetiklenir (hiçb hareket kalmazsa)

3. **Git Durumu**
   - git status temiz olmalı (commit edilmemiş çöpler yok)
   - Commit message: tek satır özet (detay body yok)

4. **Screenshot Kanıtı** (Release commits için)
   - Gameplay screenshot (juice görünür)
   - Console screenshot (0 err/0 warn)
