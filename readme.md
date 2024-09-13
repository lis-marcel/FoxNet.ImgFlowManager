# jak masz problemy z uruchomieniem Docker na Windows, uruchom a poziomu admin, a potem przeloguj usera
# konto usera pobierz z whoami
net localgroup docker-users "mk-marcin-hp\mlis" /ADD

# tworzenie obrazu
docker build -t hawix/foxsky-img-renamer -f Dockerfile .
docker buildx build --platform linux/amd64,linux/arm64 -t hawix/foxsky-img-renamer -f dockerfile .
docker buildx build -t hawix/foxsky-img-renamer -f dockerfile .

# tworzenie kontenera
docker create --name renamer hawix/foxsky-img-renamer

# pojedyncze uruchomienie kontenera ( kontener zostanie usuniety po wykonaniu --rm) z uzyciem woluminow
docker run -it --rm -v c:\temp\in:/src -v c:\temp\out:/dst hawix/foxsky-img-renamer 

# uruchomienie kontenera do inspekcji z wlasnym punktem wejscia
docker run -d --entrypoint sleep hawix/foxsky-img-renamer 3600

# popchniecie obrazu do galerii
docker push hawix/foxsky-img-renamer 