#!/usr/bin/env python3
"""
Script generador y verificador del Plan de Implementación de PharmaLink.
Analiza la estructura del proyecto en disco, verifica la presencia de archivos clave
de cada etapa (0 a 9) y muestra el estado y porcentaje de avance.
Opcionalmente actualiza el archivo PLAN_DE_IMPLEMENTACION.md con el progreso detectado.
"""

import os
import sys
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parent.parent

ETAPAS = [
    {
        "id": 0,
        "nombre": "Inspección y Alineación de Requisitos",
        "requisitos": [
            ("docs/PROJECT_BRIEF.md", "Project Brief existente"),
            ("docs/ARCHITECTURE.md", "Arquitectura MCS documentada"),
            ("README.md", "README principal del proyecto"),
            ("docs/decisions/ADR-001-adopcion-arquitectura-mcs-canonica.md", "ADRs base presentes")
        ]
    },
    {
        "id": 1,
        "nombre": "Esqueleto del Proyecto y Dependencias",
        "requisitos": [
            ("PharmaLink/PharmaLink.csproj", "Proyecto Web API creado"),
            ("PharmaLink/Program.cs", "Punto de entrada Program.cs"),
            ("PharmaLink/Context", "Directorio Context/"),
            ("PharmaLink/Controllers", "Directorio Controllers/"),
            ("PharmaLink/DTOs", "Directorio DTOs/"),
            ("PharmaLink/Interfaces", "Directorio Interfaces/"),
            ("PharmaLink/Models", "Directorio Models/"),
            ("PharmaLink/Services", "Directorio Services/")
        ]
    },
    {
        "id": 2,
        "nombre": "Modelos POCO y Persistencia EF Core",
        "requisitos": [
            ("PharmaLink/Models/Usuario.cs", "Modelo Usuario"),
            ("PharmaLink/Models/Medicamento.cs", "Modelo Medicamento"),
            ("PharmaLink/Models/Insumo.cs", "Modelo Insumo"),
            ("PharmaLink/Models/Dispensacion.cs", "Modelo Dispensacion"),
            ("PharmaLink/Models/Reposicion.cs", "Modelo Reposicion"),
            ("PharmaLink/Models/Entrega.cs", "Modelo Entrega"),
            ("PharmaLink/Context/PharmaLinkContext.cs", "PharmaLinkContext EF Core")
        ]
    },
    {
        "id": 3,
        "nombre": "Autenticación JWT y Módulo de Usuarios",
        "requisitos": [
            ("PharmaLink/DTOs/Auth/RegistroDTO.cs", "DTO Registro"),
            ("PharmaLink/DTOs/Auth/LoginDTO.cs", "DTO Login"),
            ("PharmaLink/DTOs/Auth/AuthResponseDTO.cs", "DTO AuthResponse"),
            ("PharmaLink/Interfaces/IAuthService.cs", "Interfaz IAuthService"),
            ("PharmaLink/Services/AuthService.cs", "Implementación AuthService"),
            ("PharmaLink/Controllers/AuthController.cs", "Controlador AuthController")
        ]
    },
    {
        "id": 4,
        "nombre": "CRUD Medicamentos e Insumos + DTOs",
        "requisitos": [
            ("PharmaLink/Interfaces/IMedicamentoService.cs", "Interfaz IMedicamentoService"),
            ("PharmaLink/Services/MedicamentoDbService.cs", "Servicio MedicamentoDbService"),
            ("PharmaLink/Controllers/MedicamentosController.cs", "MedicamentosController"),
            ("PharmaLink/Interfaces/IInsumoService.cs", "Interfaz IInsumoService"),
            ("PharmaLink/Services/InsumoDbService.cs", "Servicio InsumoDbService"),
            ("PharmaLink/Controllers/InsumosController.cs", "InsumosController")
        ]
    },
    {
        "id": 5,
        "nombre": "Integración Externa API Hospital",
        "requisitos": [
            ("PharmaLink/Interfaces/IHospitalApiClient.cs", "Interfaz IHospitalApiClient"),
            ("PharmaLink/ExternalServices/HospitalApiClient.cs", "Implementación HospitalApiClient")
        ]
    },
    {
        "id": 6,
        "nombre": "Flujo Transaccional de Dispensaciones",
        "requisitos": [
            ("PharmaLink/DTOs/Dispensaciones/DispensacionCreateDTO.cs", "DTO DispensacionCreate"),
            ("PharmaLink/Interfaces/IDispensacionService.cs", "Interfaz IDispensacionService"),
            ("PharmaLink/Services/DispensacionDbService.cs", "Servicio transaccional DispensacionDbService"),
            ("PharmaLink/Controllers/DispensacionesController.cs", "DispensacionesController")
        ]
    },
    {
        "id": 7,
        "nombre": "Flujo de Reposiciones e Inventario",
        "requisitos": [
            ("PharmaLink/DTOs/Reposiciones/ReposicionCreateDTO.cs", "DTO ReposicionCreate"),
            ("PharmaLink/Interfaces/IReposicionService.cs", "Interfaz IReposicionService"),
            ("PharmaLink/Services/ReposicionDbService.cs", "Servicio ReposicionDbService"),
            ("PharmaLink/Controllers/ReposicionesController.cs", "ReposicionesController")
        ]
    },
    {
        "id": 8,
        "nombre": "Pipeline de Middlewares, Errores y Logs",
        "requisitos": [
            ("PharmaLink/Middleware/ExceptionMiddleware.cs", "ExceptionMiddleware global"),
            ("PharmaLink/Middleware/ApiKeyMiddleware.cs", "ApiKeyMiddleware X-API-Key")
        ]
    },
    {
        "id": 9,
        "nombre": "Testing Automatizado y Cierre",
        "requisitos": [
            ("PharmaLink.Tests/PharmaLink.Tests.csproj", "Proyecto de Pruebas Unitarias")
        ]
    }
]

def verificar_etapas():
    total_checks = 0
    checks_completados = 0
    etapas_completas = 0

    print("=================================================================")
    print(" 🏥 ESTADO DEL PLAN DE IMPLEMENTACIÓN — PHARMALINK (MCS v2)")
    print("=================================================================\n")

    for etapa in ETAPAS:
        etapa_id = etapa["id"]
        nombre = etapa["nombre"]
        reqs = etapa["requisitos"]
        
        cumplidos_etapa = 0
        for path_str, desc in reqs:
            total_checks += 1
            full_path = ROOT_DIR / path_str
            if full_path.exists():
                cumplidos_etapa += 1
                checks_completados += 1

        pct_etapa = (cumplidos_etapa / len(reqs)) * 100 if reqs else 0
        
        if pct_etapa == 100:
            badge = "✅ COMPLETADA"
            etapas_completas += 1
        elif pct_etapa > 0:
            badge = f"🔄 EN PROGRESO ({cumplidos_etapa}/{len(reqs)})"
        else:
            badge = f"⏳ PENDIENTE   (0/{len(reqs)})"

        print(f"Etapa {etapa_id}: {nombre.ljust(44)} {badge}")
        for path_str, desc in reqs:
            full_path = ROOT_DIR / path_str
            simbolo = "✓" if full_path.exists() else "✗"
            color_simbolo = f"[{simbolo}]"
            print(f"   {color_simbolo} {desc} ({path_str})")
        print()

    progreso_total = (checks_completados / total_checks) * 100 if total_checks else 0
    print("-----------------------------------------------------------------")
    print(f"Progreso Total de Componentes: {checks_completados}/{total_checks} ({progreso_total:.1f}%)")
    print(f"Etapas Finalizadas al 100%:   {etapas_completas}/{len(ETAPAS)}")
    print("-----------------------------------------------------------------")
    print("\n👉 Para consultar el detalle y ejecutar el siguiente paso:")
    print("   Consulte 'PLAN_DE_IMPLEMENTACION.md' o inicie con la Etapa 1:")
    print("   $ dotnet new webapi -n PharmaLink --use-controllers\n")

if __name__ == "__main__":
    verificar_etapas()
