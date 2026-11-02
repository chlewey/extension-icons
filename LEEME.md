# Extension Icons

[![Licencia: MIT](https://img.shields.io/badge/Licencia-MIT-yellow.svg)](LICENSE)
[![Plataforma](https://img.shields.io/badge/plataforma-Windows%2011-0078D6)](#)

Una aplicación para Windows 11 que asigna a cada extensión de archivo un
ícono coherente y correcto, independiente de la aplicación que en ese momento
la reclame como suya.

*Read this in English: [README.md](README.md)*

## El problema

Windows resuelve el ícono de una extensión de archivo según cuál aplicación
se haya registrado por última vez como manejadora de ese formato. En la
práctica, esto hace que muchos tipos de archivo terminen con un ícono que no
tiene nada que ver con el formato en sí:

- `.htm` y `.html` aparecen como "documentos de Edge", aunque un archivo HTML
  no es un formato específico de Edge.
- `.xml`, `.csv`, `.json` y otros formatos de uso general son renombrados
  sistemáticamente con el ícono de la última aplicación que se instaló como
  su manejadora.
- Instalar una aplicación "agresiva" puede apropiarse silenciosamente de
  íconos y asociaciones de extensiones sobre las que no tiene ninguna
  reivindicación especial.

Extension Icons trata el ícono como una propiedad del **tipo de archivo**, no
de la aplicación que casualmente está registrada para él en este momento — y
permite decidir, por extensión, qué aplicación instalada debe encargarse de
cada acción.

## Qué hace

- **Íconos distintos por tipo de archivo.** Mantiene un ícono coherente para
  las extensiones comunes, sin importar qué aplicación posea la asociación
  en un momento dado.
- **Consciente de capacidades.** Reconoce qué aplicaciones instaladas pueden
  manejar una extensión dada, no solo cuál es la predeterminada actual.
- **Predeterminados por acción.** Permite elegir, por tipo de archivo, qué
  aplicación debe usarse para abrir, editar, editar como texto o visualizar
  un archivo.
- **Modo de reparación ("fix mode").** Revierte íconos y asociaciones a un
  estado guardado previamente — útil justo después de instalar una
  aplicación que reclama agresivamente tipos de archivo que no le
  corresponden.
- **Temas.** Admite conjuntos de íconos por tipo de archivo, incluyendo temas
  descargados desde repositorios de la comunidad.

## Estado

En desarrollo temprano. Consulte [`docs/ROADMAP.md`](docs/ROADMAP.md) (en
inglés) para el plan de desarrollo completo, las decisiones de arquitectura y
el desglose en MVP / v1.0 / lista de deseos.

## Stack tecnológico

- C# / .NET 8
- WPF (MVVM)
- API de Registro y Shell de Windows (`HKEY_CLASSES_ROOT`, `UserChoice` por
  usuario)

## Primeros pasos

Requiere el [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
y Visual Studio 2022 (o posterior) con la carga de trabajo "Desarrollo de
escritorio con .NET".

```powershell
git clone https://github.com/chlewey/extension-icons.git
cd extension-icons
dotnet build
```

Abra `ExtensionIcons.sln` en Visual Studio para ejecutar o depurar la
aplicación.

## Idiomas

A partir de la versión 1.0, la interfaz admitirá **inglés** y **español**,
seleccionables desde la aplicación y con el idioma del sistema operativo
como valor predeterminado.

## Licencia

[MIT](LICENSE)
