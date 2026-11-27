# PWA de Primal Echo

Esta carpeta contiene el código fuente de la aplicación web de Primal Echo. La copia se tomó del commit `ce397b9` del [repositorio original](https://github.com/boletosscw-spec/pwa/tree/ce397b96cdeef2e49c653b6258de8760d0ad2c64).

## Ejecutar en local

Con Python 3.14, abre una terminal en `Docs/PWA` y ejecuta:

```sh
python -m venv .venv
```

Activa el entorno con `.venv\Scripts\Activate.ps1` en PowerShell o con `source .venv/bin/activate` en Bash. Luego ejecuta:

```sh
python -m pip install -r requirements.txt
python manage.py migrate
python manage.py runserver
```

Abre [http://127.0.0.1:8000/](http://127.0.0.1:8000/). Django crea una base local vacía. Las cuentas y registros existentes siguen en la base del sitio original.

La aplicación requiere un servidor Django para servir las páginas y procesar el inicio de sesión.
