using Microsoft.AspNetCore.Identity;
using PharmaLink.Models;

namespace PharmaLink.Context;

public static class DbInitializer
{
    public static void Initialize(PharmaLinkContext context)
    {
        context.Database.EnsureCreated();

        // 1. Semilla de Usuarios
        if (!context.Usuarios.Any())
        {
            var hasher = new PasswordHasher<Usuario>();

            var admin = new Usuario
            {
                NombreUsuario = "admin",
                Rol = "Administrador"
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

            var operador = new Usuario
            {
                NombreUsuario = "operador",
                Rol = "Usuario"
            };
            operador.PasswordHash = hasher.HashPassword(operador, "Operador123!");

            context.Usuarios.AddRange(admin, operador);
            context.SaveChanges();
        }

        // 2. Semilla de Medicamentos
        if (!context.Medicamentos.Any())
        {
            context.Medicamentos.AddRange(
                new Medicamento
                {
                    Nombre = "Paracetamol 500mg",
                    Presentacion = "Caja x 20 comprimidos",
                    StockDisponible = 150,
                    PrecioUnitario = 1200.50m
                },
                new Medicamento
                {
                    Nombre = "Ibuprofeno 400mg",
                    Presentacion = "Caja x 10 cápsulas blandas",
                    StockDisponible = 200,
                    PrecioUnitario = 1850.00m
                },
                new Medicamento
                {
                    Nombre = "Amoxicilina 500mg",
                    Presentacion = "Caja x 16 comprimidos",
                    StockDisponible = 80,
                    PrecioUnitario = 3400.00m
                }
            );
            context.SaveChanges();
        }

        // 3. Semilla de Insumos
        if (!context.Insumos.Any())
        {
            context.Insumos.AddRange(
                new Insumo
                {
                    Nombre = "Guantes de Látex Descartables Talle M",
                    Presentacion = "Caja x 100 unidades",
                    StockDisponible = 50,
                    PrecioUnitario = 4500.00m
                },
                new Insumo
                {
                    Nombre = "Jeringas Descartables 5ml",
                    Presentacion = "Caja x 50 unidades",
                    StockDisponible = 100,
                    PrecioUnitario = 3200.00m
                },
                new Insumo
                {
                    Nombre = "Gasas Estériles 10x10 cm",
                    Presentacion = "Paquete x 10 sobres",
                    StockDisponible = 120,
                    PrecioUnitario = 1500.00m
                }
            );
            context.SaveChanges();
        }
    }
}
