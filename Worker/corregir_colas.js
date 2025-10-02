// corregir_colas.js - Ejecutar con Node.js
const fs = require('fs');
const path = require('path');
const dir = __dirname;

// Lee todos los archivos de configuración de cola
const files = fs.readdirSync(dir).filter(file => file.startsWith('appsettings.cola_') && file.endsWith('.json'));

// Procesa cada archivo
files.forEach(file => {
  const filePath = path.join(dir, file);
  const colaNum = file.replace('appsettings.cola_', '').replace('.json', '');
  console.log(`Corrigiendo cola ${colaNum}...`);
  
  try {
    // Lee el archivo como texto
    const content = fs.readFileSync(filePath, 'utf8');
    
    // Intenta analizarlo como JSON
    try {
      let config = JSON.parse(content);
      
      // Corrige el nombre de la cola
      if (config.RabbitMQ && config.RabbitMQ.Queue !== `cola_${colaNum}`) {
        config.RabbitMQ.Queue = `cola_${colaNum}`;
        
        // Escribe el archivo con el formato correcto
        fs.writeFileSync(filePath, JSON.stringify(config, null, 2));
        console.log(`${file} corregido correctamente.`);
      } else {
        console.log(`${file} ya tiene el nombre de cola correcto.`);
      }
    } catch (parseError) {
      console.error(`Error al analizar ${file}: ${parseError.message}`);
    }
  } catch (fileError) {
    console.error(`Error al leer ${file}: ${fileError.message}`);
  }
});

console.log('Proceso completado.');