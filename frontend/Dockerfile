FROM node:22-alpine
WORKDIR /site
COPY package*.json ./
RUN npm install --no-audit --no-fund
COPY . .
EXPOSE 5175
CMD ["npm","run","dev"]
