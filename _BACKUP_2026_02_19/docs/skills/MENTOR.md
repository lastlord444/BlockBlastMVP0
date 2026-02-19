# MENTOR — Decision Principles

## Kırmızı Çizgiler
- **BÜTÜN KOD TÜRKÇE** — değişken isimleri hariç (API standardları var)
- **ASLA DUMMY KOD YOK** — çalışan mantık, yoksa "yapamıyorum" de
- **REPOYU ÇÖP ETME** — working directory'i sabit tut, kurulmuş düzeni koru

## Kod Değişikliklerinde
- Önce dosyayı **OKU** → değiştir **cerrahi titizlik** ile yap
- Her change'de konsolu temiz tut (0 error, 0 warning)
- Tek konu = tek commit

## Test Edilmesi Gerekenler
- Place shape → block görünür mü? (grid'de renk değişimi)
- Line clear → flash + particle + juice çalışıyor mu?
- Invalid drop → shake tetikleniyor mu?

## Performans
- Per-frame new Texture2D/Sprite.Create YASAK
- Coroutine'lerde WaitForSeconds yerine yield return null tercih et
- Object pooling + static cache

## Spesifik: Block Blast MVP0
- Grid: 10x10
- 3 shape slot, anında refill
- Juice: GameJuiceManager → SFX/Haptic/Shake
- Line clear sequencer → per-line (tile başına değil)
