#!/usr/bin/env bash
# Publica el sitio en internet usando esta laptop como host, por un tunel HTTPS de Cloudflare.
# Uso: ./publicar.sh [puerto]   (por defecto 8000). Ctrl+C detiene el sitio y el tunel.
set -e
cd "$(dirname "$(readlink -f "$0")")"
PUERTO="${1:-8000}"
if [ ! -x .venv/bin/gunicorn ]; then
  echo "Creando entorno virtual e instalando dependencias..."
  [ -d .venv ] || python -m venv .venv
  .venv/bin/pip install -q -r requirements.txt
fi
CF="$(command -v cloudflared || true)"
if [ -z "$CF" ]; then
  CF=".venv/bin/cloudflared"
  if [ ! -x "$CF" ]; then
    echo "Descargando cloudflared..."
    curl -fL -o "$CF" https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64
    chmod +x "$CF"
  fi
fi
[ -f .secret_key ] || .venv/bin/python -c "from django.core.management.utils import get_random_secret_key as g; print(g())" > .secret_key
export DJANGO_DEBUG=0
.venv/bin/python manage.py migrate --noinput
.venv/bin/python manage.py collectstatic --noinput > /dev/null
.venv/bin/gunicorn config.wsgi --bind "127.0.0.1:$PUERTO" --workers 2 &
SERVIDOR=$!
trap 'kill $SERVIDOR 2>/dev/null' EXIT
sleep 2
echo
echo ">>> La direccion publica aparece abajo: https://....trycloudflare.com"
echo
"$CF" tunnel --url "http://127.0.0.1:$PUERTO"
