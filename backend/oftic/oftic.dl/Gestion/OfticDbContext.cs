using Microsoft.EntityFrameworkCore;
using Datos.Entities;

namespace Datos.Gestion
{
    public class OfticDbContext : DbContext
    {
        public OfticDbContext(DbContextOptions<OfticDbContext> options) : base(options)
        {
        }

        public DbSet<PersonajeMesEntity> PersonajesMes { get; set; }
        public DbSet<PersonajeGrupoEntity> PersonajesGrupo { get; set; }
        public DbSet<CuentaEmailEntity> CuentasEmail { get; set; }
        public DbSet<CuentaEmailUsuarioEntity> CuentasEmailUsuarios { get; set; }
        public DbSet<UsuarioCuentaEntity> Usuarios { get; set; }
        public DbSet<DominioCuentaEntity> Dominios { get; set; }
        public DbSet<BrandingCuentaEntity> Brandings { get; set; }
        public DbSet<EnvioCorreoEntity> EnviosCorreo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PersonajeMesEntity>(entity =>
            {
                entity.ToTable("CTR_PERSONAJE_MES");
                entity.HasKey(e => e.IdPersonajeMes);
                entity.Property(e => e.IdPersonajeMes).HasColumnName("ID_PERSONAJE_MES");

                if (Database.ProviderName == "Oracle.EntityFrameworkCore")
                {
                    OraclePropertyBuilderExtensions.UseHiLo(entity.Property(e => e.IdPersonajeMes), "SEQ_CTR_PERSONAJE_MES");
                }
                else
                {
                    entity.Property(e => e.IdPersonajeMes)
                        .ValueGeneratedOnAdd();
                }
            });

            modelBuilder.Entity<PersonajeGrupoEntity>(entity =>
            {
                entity.ToTable("CTR_PERSONAJE_GRUPO");
                entity.HasKey(e => e.IdPersonajeGrupo);
                entity.Property(e => e.IdPersonajeGrupo).HasColumnName("ID_PERSONAJE_GRUPO");

                if (Database.ProviderName == "Oracle.EntityFrameworkCore")
                {
                    OraclePropertyBuilderExtensions.UseHiLo(entity.Property(e => e.IdPersonajeGrupo), "SEQ_CTR_PERSONAJE_GRUPO");
                }
                else
                {
                    entity.Property(e => e.IdPersonajeGrupo)
                        .ValueGeneratedOnAdd();
                }
            });

            modelBuilder.Entity<CuentaEmailEntity>(entity =>
            {
                entity.ToTable("CTR_CUENTA_EMAIL");
                entity.HasKey(e => e.IdCuenta);
                entity.Property(e => e.IdCuenta).HasColumnName("ID_CUENTA");

                if (Database.ProviderName == "Oracle.EntityFrameworkCore")
                {
                    OraclePropertyBuilderExtensions.UseHiLo(entity.Property(e => e.IdCuenta), "SEQ_CTR_CUENTA_EMAIL");
                }
                else
                {
                    entity.Property(e => e.IdCuenta).ValueGeneratedOnAdd();
                }
            });

            modelBuilder.Entity<CuentaEmailUsuarioEntity>(entity =>
            {
                entity.ToTable("CTR_CUENTA_EMAIL_USUARIO");
                entity.HasKey(e => e.IdCuentaUsuario);
                entity.Property(e => e.IdCuentaUsuario).HasColumnName("ID_CUENTA_USUARIO");

                if (Database.ProviderName == "Oracle.EntityFrameworkCore")
                {
                    OraclePropertyBuilderExtensions.UseHiLo(entity.Property(e => e.IdCuentaUsuario), "SEQ_CTR_CUENTA_EMAIL_USUARIOS");
                }
                else
                {
                    entity.Property(e => e.IdCuentaUsuario).ValueGeneratedOnAdd();
                }
            });

            modelBuilder.Entity<UsuarioCuentaEntity>(entity =>
            {
                entity.ToTable("CTR_USUARIOS");
                entity.HasKey(e => e.IdUsuario);
                entity.Property(e => e.IdUsuario).HasColumnName("ID_USUARIO");
            });

            modelBuilder.Entity<DominioCuentaEntity>(entity =>
            {
                entity.ToTable("CTR_DOMINIOS");
                entity.HasKey(e => e.IdDominio);
                entity.Property(e => e.IdDominio).HasColumnName("ID_DOMINIO");
            });

            modelBuilder.Entity<BrandingCuentaEntity>(entity =>
            {
                entity.ToTable("CTR_BRANDING");
                entity.HasKey(e => e.IdBranding);
                entity.Property(e => e.IdBranding).HasColumnName("ID_BRANDING");
            });

            modelBuilder.Entity<EnvioCorreoEntity>(entity =>
            {
                entity.ToTable("CTR_ENVIO_CORREO");
                entity.HasKey(e => e.IdEnvio);
                entity.Property(e => e.IdEnvio).HasColumnName("ID_ENVIO");

                if (Database.ProviderName == "Oracle.EntityFrameworkCore")
                {
                    OraclePropertyBuilderExtensions.UseHiLo(entity.Property(e => e.IdEnvio), "SEQ_CTR_ENVIO_CORREO");
                }
                else
                {
                    entity.Property(e => e.IdEnvio).ValueGeneratedOnAdd();
                }
            });
        }
    }
}
