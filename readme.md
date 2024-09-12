# jak masz problemy z uruchomieniem Docker na Windows, uruchom a poziomu admin, a potem przeloguj usera
# konto usera pobierz z whoami
net localgroup docker-users "mk-marcin-hp\mlis" /ADD

# tworzenie obrazu
docker build -t foxsky-img-renamer -f Dockerfile .

# tworzenie kontenera
docker create --name renamer foxsky-img-renamer

# pojedyncze uruchomienie kontenera z obrazu oraz przekazanie parametrow przez linie polecen, 
# kontener zostanie usuniety po wykonaniu (--rm)
docker run -it --rm foxsky-img-renamer c:\temp\in c:\temp\out

# uzycie woluminow
docker run -it --rm -v c:\temp\in:/src -v c:\temp\out:/dst foxsky-img-renamer 

# uruchomienie kontenera do inspekcji z wlasnym punktem wejscia
docker run -d --entrypoint sleep foxsky-img-renamer 3600