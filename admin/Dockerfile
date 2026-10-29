FROM node:22-alpine
WORKDIR /site
COPY package*.json ./
RUN npm ci --no-audit --no-fund
COPY . .
EXPOSE 5174
CMD ["npm","run","dev","--","--host","0.0.0.0","--port","5174","--strictPort"]
