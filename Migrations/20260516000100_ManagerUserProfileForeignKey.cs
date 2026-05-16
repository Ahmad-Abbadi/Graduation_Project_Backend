using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graduation_Project_Backend.Migrations
{
    public partial class ManagerUserProfileForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    INSERT INTO public.user_profiles ("Id", "Name", "PhoneNumber", "PasswordHash", "TotalPoints", "Role", mall_id)
                    SELECT
                        manager.id,
                        manager.name,
                        'manager-' || replace(manager.id::text, '-', ''),
                        '',
                        0,
                        COALESCE(NULLIF(manager.role, ''), 'manager'),
                        manager.mall_id
                    FROM public.managers manager
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM public.user_profiles profile
                        WHERE profile."Id" = manager.id
                    );

                    IF EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'manager_id_fkey'
                          AND conrelid = 'public.managers'::regclass
                    ) THEN
                        ALTER TABLE public.managers DROP CONSTRAINT manager_id_fkey;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'manager_user_profile_id_fkey'
                          AND conrelid = 'public.managers'::regclass
                    ) THEN
                        ALTER TABLE public.managers
                        ADD CONSTRAINT manager_user_profile_id_fkey
                        FOREIGN KEY (id)
                        REFERENCES public.user_profiles("Id")
                        ON DELETE CASCADE;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'management_manager_id_fkey'
                          AND conrelid = 'public.management'::regclass
                    ) THEN
                        ALTER TABLE public.management DROP CONSTRAINT management_manager_id_fkey;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'management_manager_id_managers_fkey'
                          AND conrelid = 'public.management'::regclass
                    ) THEN
                        ALTER TABLE public.management
                        ADD CONSTRAINT management_manager_id_managers_fkey
                        FOREIGN KEY (manager_id)
                        REFERENCES public.managers(id)
                        ON DELETE CASCADE
                        NOT VALID;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'manager_user_profile_id_fkey'
                          AND conrelid = 'public.managers'::regclass
                    ) THEN
                        ALTER TABLE public.managers DROP CONSTRAINT manager_user_profile_id_fkey;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'manager_id_fkey'
                          AND conrelid = 'public.managers'::regclass
                    ) THEN
                        ALTER TABLE public.managers
                        ADD CONSTRAINT manager_id_fkey
                        FOREIGN KEY (id)
                        REFERENCES auth.users(id)
                        ON DELETE CASCADE;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'management_manager_id_managers_fkey'
                          AND conrelid = 'public.management'::regclass
                    ) THEN
                        ALTER TABLE public.management DROP CONSTRAINT management_manager_id_managers_fkey;
                    END IF;
                END $$;
                """);
        }
    }
}
