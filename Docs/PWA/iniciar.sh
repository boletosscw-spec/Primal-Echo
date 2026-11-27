#!/usr/bin/env bash
# Arranca el sitio en local (modo desarrollo). Uso: ./iniciar.sh [puerto]   (por defecto 8000)
set -e
cd "$(dirname "$(readlink -f "$0")")"
PUERTO="${1:-8000}"
if [ ! -x .venv/bin/gunicorn ]; then
  echo "Creando entorno virtual e instalando dependencias..."
  [ -d .venv ] || python -m venv .venv
  .venv/bin/pip install -q -r requirements.txt
fi
.venv/bin/python manage.py migrate --noinput
echo "Sitio disponible en http://127.0.0.1:$PUERTO  (Ctrl+C para detener)"
exec .venv/bin/python manage.py runserver "0.0.0.0:$PUERTO"
