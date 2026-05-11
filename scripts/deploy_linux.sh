#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   sudo ./deploy_linux.sh
# Optional env vars:
#   API_DIR=/opt/oftic/api
#   WWW_DIR=/opt/oftic/www
#   BACKUP_DIR=/opt/oftic/backups
#   API_TAR=/tmp/api-release.tar.gz
#   WEB_TAR=/tmp/web-release.tar.gz
#   API_SERVICE=oftic-api.service

API_DIR="${API_DIR:-/opt/oftic/api}"
WWW_DIR="${WWW_DIR:-/opt/oftic/www}"
BASE_DIR="$(dirname "$API_DIR")"
BACKUP_DIR="${BACKUP_DIR:-$BASE_DIR/backups}"
API_TAR="${API_TAR:-/tmp/api-release.tar.gz}"
WEB_TAR="${WEB_TAR:-/tmp/web-release.tar.gz}"
API_SERVICE="${API_SERVICE:-oftic-api.service}"
STAMP="$(date +%F_%H%M%S)"

echo "== SICOE deploy (Linux) =="
echo "API dir:   $API_DIR"
echo "WEB dir:   $WWW_DIR"
echo "API tar:   $API_TAR"
echo "WEB tar:   $WEB_TAR"
echo "API svc:   $API_SERVICE"

if [[ $EUID -ne 0 ]]; then
  echo "ERROR: ejecuta como root o con sudo."
  exit 1
fi

for d in "$API_DIR" "$WWW_DIR"; do
  if [[ ! -d "$d" ]]; then
    echo "ERROR: no existe directorio: $d"
    exit 1
  fi
done

for f in "$API_TAR" "$WEB_TAR"; do
  if [[ ! -f "$f" ]]; then
    echo "ERROR: no existe artefacto: $f"
    exit 1
  fi
done

mkdir -p "$BACKUP_DIR"

echo
echo "[1/7] Backup de API y WEB..."
tar -czf "$BACKUP_DIR/api_$STAMP.tar.gz" -C "$API_DIR" .
tar -czf "$BACKUP_DIR/www_$STAMP.tar.gz" -C "$WWW_DIR" .

echo
echo "[2/7] Deteniendo API (systemd)..."
systemctl stop "$API_SERVICE"

echo
echo "[3/7] Limpiando API (preserva appsettings*.json y uploads)..."
find "$API_DIR" -mindepth 1 -maxdepth 1 \
  ! -name 'appsettings.json' \
  ! -name 'appsettings.Development.json' \
  ! -name 'appsettings.Production.json' \
  ! -name 'uploads' \
  -exec rm -rf {} +

echo
echo "[4/7] Extrayendo nueva API..."
tar -xzf "$API_TAR" -C "$API_DIR"

echo
echo "[5/7] Reemplazando frontend..."
find "$WWW_DIR" -mindepth 1 -maxdepth 1 -exec rm -rf {} +
tar -xzf "$WEB_TAR" -C "$WWW_DIR"

# Soporta ambos formatos de artefacto:
# 1) contenido web directo (index.html, main-*.js, ...)
# 2) contenido dentro de carpeta browser/
if [[ -d "$WWW_DIR/browser" && ! -f "$WWW_DIR/index.html" ]]; then
  echo "Detectado artefacto con carpeta browser/. Aplanando a $WWW_DIR ..."
  shopt -s dotglob nullglob
  mv "$WWW_DIR/browser"/* "$WWW_DIR"/
  shopt -u dotglob nullglob
  rm -rf "$WWW_DIR/browser"
fi

echo
echo "[6/7] Ajustando permisos..."
chown -R apache:apache "$API_DIR" "$WWW_DIR"
chmod -R 755 "$API_DIR" "$WWW_DIR"

echo
echo "[7/7] Iniciando API..."
systemctl start "$API_SERVICE"
sleep 2
systemctl is-active --quiet "$API_SERVICE"

echo
echo "Verificando API..."
if curl -s -f http://127.0.0.1:7095/api/Noticia/activas > /dev/null; then
  echo "API OK (127.0.0.1:7095)"
else
  echo "API FALLO, revisa: journalctl -u $API_SERVICE -n 100 --no-pager"
fi

echo
echo "Deploy finalizado."
echo "Backups:"
echo "  $BACKUP_DIR/api_$STAMP.tar.gz"
echo "  $BACKUP_DIR/www_$STAMP.tar.gz"

