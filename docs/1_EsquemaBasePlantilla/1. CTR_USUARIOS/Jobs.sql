
/*what:*/  PKS_ADMINISTRACION.DeshabilitaUsuarios;

--Definir Intervalos
SYSDATE + 1 --1 Día
SYSDATE + 180/1440 --3Horas
SYSDATE + 120/1440 --2Horas
SYSDATE + 30/1440 --30 Minutos


begin
  sys.dbms_scheduler.create_job(job_name            => 'DESACTIVAR_USUARIOS',
                                job_type            => 'PLSQL_BLOCK',
                                job_action          => 'PKS_ADMINISTRACION.DeshabilitaUsuarios;',
                                start_date          => to_date('13-07-2024 14:23:32', 'dd-mm-yyyy hh24:mi:ss'),
                                repeat_interval     => 'Freq=Hourly;Interval=24',                               
                                end_date            => to_date(null),
                                job_class           => 'DEFAULT_JOB_CLASS',
                                enabled             => true, 
                                auto_drop           => true,                              
                                comments            => 'Jobs para desactivar usuarios');
end;
/




