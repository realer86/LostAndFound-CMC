// Syntax validation of UI modules with Babel's JSX parser (dependency of @vitejs/plugin-react).
import {createRequire} from 'node:module';
import {readFileSync} from 'node:fs';
import {resolve,dirname,join} from 'node:path';
import {fileURLToPath} from 'node:url';
const require=createRequire(import.meta.url);
const root=resolve(dirname(fileURLToPath(import.meta.url)),'..');
const nodeModules=process.env.FOUNDLY_NODE_MODULES || join(root,'frontend','node_modules');
const parser=require(join(nodeModules,'@babel/parser'));
for (const path of ['frontend/src/main.jsx','frontend/src/components/SupportInbox.jsx','admin/src/main.jsx','admin/src/SupportAdminInbox.jsx']) {
  parser.parse(readFileSync(join(root,path),'utf-8'),{sourceType:'module',plugins:['jsx']});
  console.log('PASS: JSX syntax',path);
}
