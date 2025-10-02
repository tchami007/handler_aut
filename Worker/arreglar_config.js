// arreglar_config.js - Ejecutar con Node.js
const fs = require('fs');
const path = require('path');
const dir = __dirname;

// Lee todos los archivos de configuración de cola
const files = fs.readdirSync(dir).filter(file => file.startsWith('appsettings.cola_') && file.endsWith('.json'));

// Cadena de conexión a agregar
const connectionString = {
  "ConnectionStrings": {
    "BanksysConnection": "Server=172.16.57.198;Database=BANKSYS;User Id=sa;Password=Censys2300*;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
};

// Procesa cada archivo
files.forEach(file => {
  const filePath = path.join(dir, file);
  console.log(`Procesando ${file}...`);
  
  try {
    // Lee el archivo como texto
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Intenta analizarlo como JSON
    try {
      let config = JSON.parse(content);
      
      // Verifica si ya tiene la sección ConnectionStrings
      if (!config.ConnectionStrings) {
        // Agrega la sección ConnectionStrings
        config.ConnectionStrings = connectionString.ConnectionStrings;
        
        // Escribe el archivo con el formato correcto
        fs.writeFileSync(filePath, JSON.stringify(config, null, 2));
        console.log(`${file} actualizado correctamente.`);
      } else {
        console.log(`${file} ya tiene configuración de ConnectionStrings.`);
      }
    } catch (parseError) {
      console.error(`Error al analizar ${file}: ${parseError.message}`);
      // Intenta arreglar el formato manualmente
      let fixedContent = `{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "RabbitMQ": {
    "Host": "172.16.57.184",
    "Port": 5672,
    "UserName": "prueba",
    "Password": "Censys2300*",
    "VirtualHost": "/",
    "Exchange": "handler_exchange",
    "Queue": "${file.replace('appsettings.cola_', '').replace('.json', '')}"
  },
  "ConnectionStrings": {
    "BanksysConnection": "Server=172.16.57.198;Database=BANKSYS;User Id=sa;Password=Censys2300*;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}`;
      fs.writeFileSync(filePath, fixedContent);
      console.log(`${file} fue reparado manualmente.`);
    }
  } catch (fileError) {
    console.error(`Error al leer ${file}: ${fileError.message}`);
  }
});

console.log('Proceso completado.');